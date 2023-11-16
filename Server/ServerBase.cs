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

    private int _hearBeat = 30;

    public ServerBase(int maxSessionCount = 10)
    {
        _messageRouter   = new MessageRouter();
        _networkListener = new NetworkListener(maxSessionCount);

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
                
            var action = (Action<MessagePack>)Delegate.CreateDelegate(typeof(Action<MessagePack>), this, methodInfo);
            var routeAttribute = methodInfo.GetCustomAttribute<MessageRouteAttribute>();
            _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, action);
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

    public void Start(int port)
    {
        Logger.Info($"Server Init!");
        _networkListener.Listen("0.0.0.0", port);
        
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
        foreach (var session in SessionList.Values)
        {
            if(session.SessionObject is not ClientBase client) continue;
            
            if (currentTime - client.LastPingTime >= _hearBeat)
            {
                Close(session);
            }
        }
    }
    
    public static long GetTimeStamp()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(timeSpan.TotalSeconds);
    }
}