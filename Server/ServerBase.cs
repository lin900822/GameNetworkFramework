using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using Common;
using Log;
using Network;

namespace Server;

public abstract class ServerBase<TClient> where TClient : ClientBase, new()
{
    public MessageRouter Router => _messageRouter;
    public NetworkListener NetworkListener => _networkListener;

    public Dictionary<Socket, NetworkSession> SessionList => _networkListener.SessionList;

    private MessageRouter _messageRouter;
    private NetworkListener _networkListener;

    private Stopwatch _updateStopwatch;

    protected int _targetFps = 15;
    protected int _millisecondsPerFrame;
    protected long _millisecondsPassed;
    protected long _frameCount = 0;

    private readonly ServerSettings _settings;

    protected ServerBase(ServerSettings settings)
    {
        _settings = settings;

        _messageRouter = new MessageRouter();
        _networkListener = new NetworkListener(settings.MaxSessionCount);

        _networkListener.OnSessionConnected += (session) =>
        {
            var client = new TClient();

            client.LastPingTime = TimeUtils.GetTimeStamp();
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
            if (!methodInfo.IsDefined(typeof(MessageRouteAttribute), true)) continue;

            var parameters = methodInfo.GetParameters();
            var returnType = methodInfo.ReturnType;
            var routeAttributes = methodInfo.GetCustomAttributes<MessageRouteAttribute>();

            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(ReceivedMessageInfo))
            {
                throw new ArgumentException($"MessageRoute Method \"{methodInfo.Name}\" Parameters Error!");
            }

            if (returnType == typeof(void))
            {
                var action =
                    (Action<ReceivedMessageInfo>)Delegate.CreateDelegate(typeof(Action<ReceivedMessageInfo>), this,
                        methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;
                    _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, action);
                }
            }
            else if (returnType == typeof(Task))
            {
                var func = (Func<ReceivedMessageInfo, Task>)Delegate.CreateDelegate(
                    typeof(Func<ReceivedMessageInfo, Task>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(ReceivedMessageInfo messageInfo)
                    {
                        func(messageInfo).Await(null, e => { Logger.Error(e.ToString()); });
                    }

                    _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, Handler);
                }
            }
            else if (returnType == typeof(Response))
            {
                var func = (Func<ReceivedMessageInfo, Response>)Delegate.CreateDelegate(
                    typeof(Func<ReceivedMessageInfo, Response>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, (messageInfo) =>
                    {
                        var response = func(messageInfo);
                        if (response.Message == null) return;
                        _networkListener.Send(messageInfo.Session, messageInfo.StateCode, response.Message,
                            response.StateCode);
                    });
                }
            }
            else if (returnType == typeof(Task<Response>))
            {
                var func = (Func<ReceivedMessageInfo, Task<Response>>)Delegate.CreateDelegate(
                    typeof(Func<ReceivedMessageInfo, Task<Response>>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(ReceivedMessageInfo messageInfo)
                    {
                        func(messageInfo).Await(
                            response =>
                            {
                                if (response.Message == null) return;
                                _networkListener.Send(messageInfo.Session, messageInfo.StateCode, response.Message, response.StateCode);
                            },
                            e => Logger.Error(e.ToString()));
                    }

                    _messageRouter.RegisterMessageHandler(routeAttribute.MessageId, Handler);
                }
            }
            else
            {
                throw new ArgumentException($"MessageRoute Method \"{methodInfo.Name}\" Return Type Error!");
            }
        }
    }

    public void SendMessage(NetworkSession session, uint messageId, byte[] message)
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

        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Deinit(); };

        _updateStopwatch = Stopwatch.StartNew();

        Init();

        var synchronizationContext = new GameSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        while (true)
        {
            Update();
            synchronizationContext.ProcessQueue();
        }
    }

    private void Init()
    {
        _millisecondsPerFrame = 1000 / _targetFps;

        OnInit();
    }

    private void Update()
    {
        try
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
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    private void Deinit()
    {
        OnDeinit();
        Logger.Info($"Server Deinit!");
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnDeinit()
    {
    }

    [MessageRoute(0)]
    public void OnReceivedPing(ReceivedMessageInfo receivedMessageInfo)
    {
        if (receivedMessageInfo.Session.SessionObject is not ClientBase client) return;

        client.LastPingTime = TimeUtils.GetTimeStamp();
        SendMessage(receivedMessageInfo.Session, 1, Array.Empty<byte>());
    }

    private void CheckHeartBeat()
    {
        var currentTime = TimeUtils.GetTimeStamp();
        lock (SessionList)
        {
            foreach (var session in SessionList.Values)
            {
                if (session.SessionObject is not ClientBase client) continue;

                if (currentTime - client.LastPingTime >= _settings.HeartBeat)
                {
                    Close(session);
                }
            }
        }
    }
}