using System.Net.Sockets;
using System.Reflection;
using Core.Common;
using Core.Logger;
using Core.Metrics;
using Core.Network;
using Protocol;
using Server.Prometheus;

namespace Server;

public abstract class ServerBase<TClient> where TClient : ClientBase, new()
{
    public MessageRouter Router => _messageRouter;
    public NetworkListener NetworkListener => _networkListener;

    public Dictionary<Socket, NetworkSession> SessionList => _networkListener.SessionList;

    private MessageRouter _messageRouter;
    private NetworkListener _networkListener;

    private PrometheusService _prometheusService;
    private long _lastSyncPrometheusTimeMs;
    private const int SyncPrometheusInterval = 1000;

    private long _startTimeMs;
    private long _lastFrameTimeMs;

    protected int _targetFps = 20;
    protected int _millisecondsPerFrame;
    protected long _millisecondsPassed;
    protected long _frameCount = 0;

    private readonly ServerSettings _settings;

    protected ServerBase(ServerSettings settings)
    {
        _settings = settings;

        _messageRouter = new MessageRouter();
        _networkListener = new NetworkListener(settings.MaxSessionCount);

        _prometheusService = new PrometheusService();
        _prometheusService.Start();

        _networkListener.OnSessionConnected += OnSessionConnected;
        _networkListener.OnReceivedMessage  += OnReceivedMessage;
        RegisterMessageHandlers();
    }

    ~ServerBase()
    {
        _networkListener.OnSessionConnected -= OnSessionConnected;
        _networkListener.OnReceivedMessage  -= OnReceivedMessage;
        
        _prometheusService.Stop();
    }

    private void OnSessionConnected(NetworkSession session)
    {
        var client = new TClient();

        client.LastPingTime   = TimeUtils.GetTimeStamp();
        session.SessionObject = client;
    }
    
    private void OnReceivedMessage(ReceivedMessageInfo receivedMessageInfo)
    {
        if (receivedMessageInfo.Session.SessionObject is ClientBase clientBase)
        {
            clientBase.LastPingTime = TimeUtils.GetTimeStamp();
        }
        
        _messageRouter.ReceiveMessage(receivedMessageInfo);
    }

    #region - Message Route -

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
                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, action);
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
                        func(messageInfo).Await(null, e => { Log.Error(e.ToString()); });
                    }

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, Handler);
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

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, (messageInfo) =>
                    {
                        var response = func(messageInfo);
                        if (response.Message == null) return;
                        messageInfo.Communicator.Send((ushort)routeAttribute.MessageId, response.Message, true, messageInfo.RequestId);
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
                                messageInfo.Communicator.Send((ushort)routeAttribute.MessageId, response.Message, true, messageInfo.RequestId);
                            },
                            e => Log.Error(e.ToString()));
                    }

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, Handler);
                }
            }
            else
            {
                throw new ArgumentException($"MessageRoute Method \"{methodInfo.Name}\" Return Type Error!");
            }
        }
    }

    #endregion
    
    public void SendMessage(NetworkCommunicator session, ushort messageId, byte[] message)
    {
        _networkListener.Send(session, messageId, message);
    }

    public void Start()
    {
        Log.Info($"{_settings.ServerName} (Id:{_settings.ServerId}) Init!");
        _networkListener.Listen("0.0.0.0", _settings.Port);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Deinit(); };

        _startTimeMs = TimeUtils.TimeSinceAppStart;

        var synchronizationContext = new GameSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        // Start Life Cycle
        Init();
        
        while (true)
        {
            Update();
            synchronizationContext.ProcessQueue();
            Thread.Sleep(1);
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
            _networkListener.Update();
            
            _millisecondsPassed = TimeUtils.TimeSinceAppStart - _startTimeMs;

            while (_millisecondsPassed - _frameCount * _millisecondsPerFrame >= _millisecondsPerFrame)
            {
                OnFixedUpdate();
                ++_frameCount;
                CheckHeartBeat();
                SyncPrometheus();
                
                var deltaTime = (TimeUtils.TimeSinceAppStart - _lastFrameTimeMs) / 1000f;
                SystemMetrics.FPS = 1f / deltaTime;
                _prometheusService.UpdateFPS(SystemMetrics.FPS);
                _lastFrameTimeMs = TimeUtils.TimeSinceAppStart;
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private void Deinit()
    {
        OnDeinit();
        Log.Info($"Server Deinit!");
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnFixedUpdate()
    {
    }

    protected virtual void OnDeinit()
    {
    }

    [MessageRoute(MessageId.HeartBeat)]
    public void OnReceivedPing(ReceivedMessageInfo receivedMessageInfo)
    {
        if (receivedMessageInfo.Session.SessionObject is not ClientBase client) return;

        client.LastPingTime = TimeUtils.GetTimeStamp();
        SendMessage(receivedMessageInfo.Communicator, 1, Array.Empty<byte>());
    }

    private void CheckHeartBeat()
    {
        var currentTime = TimeUtils.GetTimeStamp();
        lock (SessionList)
        {
            foreach (var session in SessionList.Values)
            {
                if (session.SessionObject is not ClientBase client) continue;

                if (currentTime - client.LastPingTime >= _settings.HeartBeatInterval)
                {
                    Log.Info($"{session.Socket.RemoteEndPoint} Heart Beat Time Out!");
                    _networkListener.Close(session.Socket);
                }
            }
        }
    }

    private void SyncPrometheus()
    {
        if(TimeUtils.TimeSinceAppStart - _lastSyncPrometheusTimeMs < SyncPrometheusInterval) return;
        _lastSyncPrometheusTimeMs = TimeUtils.TimeSinceAppStart;

        _prometheusService.UpdateSessionCount(_networkListener.ConnectionCount);
        _prometheusService.UpdateRemainMessageCount(SystemMetrics.RemainMessageCount);
        _prometheusService.UpdateHandledMessagePerSecond(SystemMetrics.HandledMessageCount);
        SystemMetrics.HandledMessageCount = 0;
    }
}