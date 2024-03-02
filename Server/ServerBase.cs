using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using Core.Common;
using Core.Logger;
using Core.Metrics;
using Core.Network;
using Protocol;
using Server.Prometheus;

namespace Server;

public abstract class ServerBase<TClient> where TClient : ClientBase<TClient>, new()
{
    public MessageRouter   Router          => _messageRouter;
    public NetworkListener NetworkListener => _networkListener;

    public readonly Dictionary<NetworkSession, TClient> ClientList;

    private MessageRouter   _messageRouter;
    private NetworkListener _networkListener;

    private       PrometheusService _prometheusService;
    private       long              _lastSyncPrometheusTimeMs;
    private const int               SyncPrometheusInterval = 1000;

    private long _startTimeMs;
    private long _lastFrameTimeMs;

    protected int  _targetFps = 20;
    protected int  _millisecondsPerFrame;
    protected long _millisecondsPassed;
    protected long _frameCount = 0;

    private readonly ServerSettings _settings;

    protected ServerBase(ServerSettings settings)
    {
        _settings = settings;

        ClientList = new Dictionary<NetworkSession, TClient>();

        _messageRouter   = new MessageRouter();
        _networkListener = new NetworkListener(settings.MaxSessionCount);

        _prometheusService = new PrometheusService();
        _prometheusService.Start();

        _networkListener.OnSessionConnected    += OnSessionConnected;
        _networkListener.OnSessionDisconnected += OnSessionDisconnected;
        _networkListener.OnReceivedMessage     += OnReceivedMessage;
        RegisterMessageHandlers();
    }

    ~ServerBase()
    {
        _networkListener.OnSessionConnected    -= OnSessionConnected;
        _networkListener.OnSessionDisconnected -= OnSessionDisconnected;
        _networkListener.OnReceivedMessage     -= OnReceivedMessage;

        _prometheusService.Stop();
    }

    #region - NetworkListner Events -

    private void OnSessionConnected(NetworkSession session)
    {
        var client = new TClient();
        client.SetServer(this);
        client.Init();

        client.LastPingTime   = TimeUtils.GetTimeStamp();
        session.SessionObject = client;

        ClientList.TryAdd(session, client);
    }

    private void OnSessionDisconnected(NetworkSession session)
    {
        if (!ClientList.TryGetValue(session, out var client))
            return;
        client.Deinit();
        ClientList.Remove(session);
    }

    private void OnReceivedMessage(NetworkCommunicator communicator, ReceivedMessageInfo receivedMessageInfo)
    {
        if (ClientList.TryGetValue((NetworkSession)communicator, out var client))
        {
            client.LastPingTime = TimeUtils.GetTimeStamp();
        }

        _messageRouter.ReceiveMessage(communicator, receivedMessageInfo);
    }

    #endregion

    #region - Message Route -

    private void RegisterMessageHandlers()
    {
        var methodInfos = GetType().GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            if (!methodInfo.IsDefined(typeof(MessageRouteAttribute), true)) continue;

            var parameters      = methodInfo.GetParameters();
            var returnType      = methodInfo.ReturnType;
            var routeAttributes = methodInfo.GetCustomAttributes<MessageRouteAttribute>();

            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(NetworkCommunicator) || parameters[1].ParameterType != typeof(ReceivedMessageInfo))
            {
                throw new ArgumentException($"MessageRoute Method \"{methodInfo.Name}\" Parameters Error!");
            }

            if (returnType == typeof(void))
            {
                var action =
                    (Action<NetworkCommunicator, ReceivedMessageInfo>)Delegate.CreateDelegate(
                        typeof(Action<NetworkCommunicator, ReceivedMessageInfo>), this,
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
                var func = (Func<NetworkCommunicator, ReceivedMessageInfo, Task>)Delegate.CreateDelegate(
                    typeof(Func<NetworkCommunicator, ReceivedMessageInfo, Task>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(NetworkCommunicator communicator, ReceivedMessageInfo messageInfo)
                    {
                        func(communicator, messageInfo).Await(null, e => { Log.Error(e.ToString()); });
                    }

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, Handler);
                }
            }
            else if (returnType == typeof(Response))
            {
                var func = (Func<NetworkCommunicator, ReceivedMessageInfo, Response>)Delegate.CreateDelegate(
                    typeof(Func<NetworkCommunicator, ReceivedMessageInfo, Response>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId,
                        (communicator, messageInfo) =>
                        {
                            var response = func(communicator, messageInfo);
                            if (response.Message == null) return;
                            communicator.Send((ushort)routeAttribute.MessageId, response.Message, true,
                                messageInfo.RequestId);
                        });
                }
            }
            else if (returnType == typeof(Task<Response>))
            {
                var func = (Func<NetworkCommunicator, ReceivedMessageInfo, Task<Response>>)Delegate.CreateDelegate(
                    typeof(Func<NetworkCommunicator, ReceivedMessageInfo, Task<Response>>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(NetworkCommunicator communicator, ReceivedMessageInfo messageInfo)
                    {
                        func(communicator, messageInfo).Await(
                            response =>
                            {
                                if (response.Message == null) return;
                                communicator.Send((ushort)routeAttribute.MessageId, response.Message, true,
                                    messageInfo.RequestId);
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

    #region - Life Cycle -

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
            _networkListener.AddSessions();
            _networkListener.HandleMessages();

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

            SystemMetrics.RemainMessageCount = 0;
            _networkListener.CloseSessions();
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

    #endregion

    #region - Heart Beat -

    [MessageRoute(MessageId.HeartBeat)]
    public void OnReceivedPing(NetworkCommunicator communicator, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!ClientList.TryGetValue((NetworkSession)communicator, out var client))
        {
            return;
        }

        client.LastPingTime = TimeUtils.GetTimeStamp();
        communicator.Send(1, Array.Empty<byte>());
    }

    private void CheckHeartBeat()
    {
        var currentTime = TimeUtils.GetTimeStamp();
        lock (ClientList)
        {
            foreach (var session in ClientList.Keys)
            {
                var client = ClientList[session];

                if (currentTime - client.LastPingTime >= _settings.HeartBeatInterval)
                {
                    Log.Info($"{session.Socket.RemoteEndPoint} Heart Beat Time Out!");
                    _networkListener.Close(session.Socket);
                }
            }
        }
    }

    #endregion

    #region - Other -

    public void BroadcastMessage(ushort messageId, byte[] message)
    {
        foreach (var session in ClientList.Keys)
        {
            session.Send(messageId, message);
        }
    }

    #endregion

    #region - Metrics -

    private void SyncPrometheus()
    {
        if (TimeUtils.TimeSinceAppStart - _lastSyncPrometheusTimeMs < SyncPrometheusInterval) return;
        _lastSyncPrometheusTimeMs = TimeUtils.TimeSinceAppStart;

        _prometheusService.UpdateSessionCount(_networkListener.ConnectionCount);
        _prometheusService.UpdateRemainMessageCount(SystemMetrics.RemainMessageCount);
        _prometheusService.UpdateHandledMessagePerSecond(SystemMetrics.HandledMessageCount);
        SystemMetrics.HandledMessageCount = 0;

        var gc0 = GC.CollectionCount(0) - SystemMetrics.LastGC0;
        var gc1 = GC.CollectionCount(1) - SystemMetrics.LastGC1;
        var gc2 = GC.CollectionCount(2) - SystemMetrics.LastGC2;

        SystemMetrics.LastGC0 = GC.CollectionCount(0);
        SystemMetrics.LastGC1 = GC.CollectionCount(1);
        SystemMetrics.LastGC2 = GC.CollectionCount(2);

        var memoryUsed = Process.GetCurrentProcess().WorkingSet64;
        _prometheusService.UpdateMemory(memoryUsed / (1024 * 1024));

        _prometheusService.UpdateGC0PerSecond(gc0);
        _prometheusService.UpdateGC1PerSecond(gc1);
        _prometheusService.UpdateGC2PerSecond(gc2);
    }

    #endregion
}