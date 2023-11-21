using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using Log;
using Network;

namespace Server;

public abstract class ServerBase<TClient> where TClient : ClientBase, new()
{
    public MessageRouter   Router          => _messageRouter;
    public NetworkListener NetworkListener => _networkListener;
    
    public Dictionary<Socket, NetworkSession> SessionList => _networkListener.SessionList; 
    
    private MessageRouter   _messageRouter;
    private NetworkListener _networkListener;

    private Stopwatch _updateStopwatch;
    
    protected int  _targetFps = 15;
    protected int  _millisecondsPerFrame;
    protected long _millisecondsPassed;
    protected long _frameCount = 0;

    private readonly ServerSettings _settings;

    protected ServerBase(ServerSettings settings)
    {
        _settings = settings;
        
        _messageRouter   = new MessageRouter();
        _networkListener = new NetworkListener(settings.MaxSessionCount);

        _networkListener.OnConnected += (session) =>
        {
            var client = new TClient();
            
            client.LastPingTime   = GetTimeStamp();
            session.SessionObject = client;
        };
        _networkListener.OnReceivedMessage += _messageRouter.ReceiveMessage;
        RegisterMessageHandlers();
    }
    
    private void RegisterMessageHandlers()
    {
        var methodInfos = GetType().GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            if(!methodInfo.IsDefined(typeof(MessageRouteAttribute), true)) continue;

            var parameters     = methodInfo.GetParameters();
            var returnType     = methodInfo.ReturnType;
            var routeAttribute = methodInfo.GetCustomAttribute<MessageRouteAttribute>();

            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(MessagePack))
            {
                throw new ArgumentException($"MessageRoute {routeAttribute.MessageId} Method \"{methodInfo.Name}\" Parameters Error!");
            }

            if (returnType == typeof(void))
            {
                var action = (Action<MessagePack>)Delegate.CreateDelegate(typeof(Action<MessagePack>), this, methodInfo);
                _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, action);
            }
            else if(returnType == typeof(Task))
            {
                var action = (Func<MessagePack, Task>)Delegate.CreateDelegate(typeof(Func<MessagePack, Task>), this, methodInfo);
                _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, async (messagePack) =>
                {
                    await action(messagePack);
                });
            }
            else
            {
                throw new ArgumentException($"MessageRoute {routeAttribute.MessageId} Method \"{methodInfo.Name}\" Return Type Error!");
            }
        }
    }
    
    public void Send(NetworkSession session, UInt16 messageId, byte[] message)
    {
        _networkListener.Send(session, messageId, message);
    }

    public void Close(NetworkSession session)
    {
        if (session == null) return;
        if (session.Socket == null) return;
        
        _networkListener.Close(session.Socket);
    }

    public void Start()
    {
        Logger.Info($"{_settings.ServerName} (Id:{_settings.ServerId}) Init!");
        _networkListener.Listen("0.0.0.0", _settings.Port);
        
        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Deinit();};

        _updateStopwatch = Stopwatch.StartNew();
        
        Init();
        
        while (true)
        {
            Update();
        }
    }

    private void Init()
    {
        _millisecondsPerFrame = 1000 / _targetFps;
        
        OnInit();
    }

    private void Update()
    { 
        _messageRouter.OnUpdateLogic();

        _millisecondsPassed = _updateStopwatch.ElapsedMilliseconds;
        while (_millisecondsPassed - _frameCount * _millisecondsPerFrame >= _millisecondsPerFrame)
        {
            ++_frameCount;
            OnUpdate();
            CheckHeartBeat();
        }
    }

    private void Deinit()
    {
        OnDeinit();
        Logger.Info($"Server Deinit!");
    }

    protected virtual void OnInit()   {}
    protected virtual void OnUpdate() {}
    protected virtual void OnDeinit() {}

    [MessageRoute(0)]
    public void OnReceivedPing(MessagePack messagePack)
    {
        if (messagePack.Session.SessionObject is not ClientBase client) return;
        
        client.LastPingTime = GetTimeStamp();
        Send(messagePack.Session, 1, Array.Empty<byte>());
    }

    private void CheckHeartBeat()
    {
        var currentTime = GetTimeStamp();
        lock (SessionList)
        {
            foreach (var session in SessionList.Values)
            {
                if(session.SessionObject is not ClientBase client) continue;
            
                if (currentTime - client.LastPingTime >= _settings.HeartBeat)
                {
                    Close(session);
                }
            }
        }
    }
    
    public static long GetTimeStamp()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(timeSpan.TotalSeconds);
    }
}