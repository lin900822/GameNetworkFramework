using System.Diagnostics;
using System.Reflection;
using Core.Common;
using Core.Logger;
using Core.Metrics;
using Core.Network;
using Server.Prometheus;
using Shared;

namespace Server;

public abstract class ServerBase<TClient> where TClient : ClientBase<TClient>, new()
{
    public readonly Dictionary<NetworkCommunicator, TClient> ClientList;

    private MessageRouter<TClient> _messageRouter;
    private NetworkListener        _networkListener;

    private       PrometheusService _prometheusService;
    private       long              _lastSyncPrometheusTimeMs;
    private const int               SyncPrometheusInterval = 1000;

    private long _startTimeMs;
    private long _lastFrameTimeMs;

    protected int  _millisecondsPerFrame;
    protected long _millisecondsPassed;
    protected long _frameCount = 0;

    protected readonly ServerSettings _settings;

    protected ServerBase(ServerSettings settings)
    {
        _settings = settings;

        ClientList = new Dictionary<NetworkCommunicator, TClient>();

        _messageRouter   = new MessageRouter<TClient>();
        _networkListener = new NetworkListener(settings.MaxConnectionCount);

        _prometheusService = new PrometheusService();
        _prometheusService.Start(_settings.PrometheusPort);

        _networkListener.OnCommunicatorConnected    += OnCommunicatorConnected;
        _networkListener.OnCommunicatorDisconnected += OnCommunicatorDisconnected;
        _networkListener.OnReceivedMessage          += OnReceivedMessage;
        RegisterMessageHandlers();
    }

    ~ServerBase()
    {
        _networkListener.OnCommunicatorConnected    -= OnCommunicatorConnected;
        _networkListener.OnCommunicatorDisconnected -= OnCommunicatorDisconnected;
        _networkListener.OnReceivedMessage          -= OnReceivedMessage;

        _prometheusService.Stop();
    }

    #region - NetworkListner Events -

    private void OnCommunicatorConnected(NetworkCommunicator communicator)
    {
        var client = new TClient();
        client.Init(this, communicator);

        client.LastPingTime = TimeUtils.GetTimeStamp();

        ClientList.TryAdd(communicator, client);
        OnClientConnected(client);
    }

    private void OnCommunicatorDisconnected(NetworkCommunicator communicator)
    {
        if (!ClientList.TryGetValue(communicator, out var client))
            return;

        OnClientDisconnected(client);
        client.Deinit();
        ClientList.Remove(communicator);
    }

    private void OnReceivedMessage(NetworkCommunicator communicator, ReceivedMessageInfo receivedMessageInfo)
    {
        if (ClientList.TryGetValue(communicator, out var client))
        {
            client.LastPingTime = TimeUtils.GetTimeStamp();
        }

        _messageRouter.ReceiveMessage(client, receivedMessageInfo);
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

            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(TClient) ||
                parameters[1].ParameterType != typeof(ReceivedMessageInfo))
            {
                throw new ArgumentException($"MessageRoute Method \"{methodInfo.Name}\" Parameters Error!");
            }

            if (returnType == typeof(void))
            {
                var action =
                    (Action<TClient, ReceivedMessageInfo>)Delegate.CreateDelegate(
                        typeof(Action<TClient, ReceivedMessageInfo>), this,
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
                var func = (Func<TClient, ReceivedMessageInfo, Task>)Delegate.CreateDelegate(
                    typeof(Func<TClient, ReceivedMessageInfo, Task>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(TClient client, ReceivedMessageInfo messageInfo)
                    {
                        func(client, messageInfo).Await(null, e => { Log.Error(e.ToString()); });
                    }

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId, Handler);
                }
            }
            else if (returnType == typeof(ByteBuffer))
            {
                var func = (Func<TClient, ReceivedMessageInfo, ByteBuffer>)Delegate.CreateDelegate(
                    typeof(Func<TClient, ReceivedMessageInfo, ByteBuffer>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    _messageRouter.RegisterMessageHandler((ushort)routeAttribute.MessageId,
                        (client, messageInfo) =>
                        {
                            var response = func(client, messageInfo);
                            if (response == null)
                                return;
                            client.Send((ushort)routeAttribute.MessageId, response, true,
                                messageInfo.RequestId);
                            ByteBufferPool.Shared.Return(response);
                        });
                }
            }
            else if (returnType == typeof(Task<ByteBuffer>))
            {
                var func = (Func<TClient, ReceivedMessageInfo, Task<ByteBuffer>>)Delegate.CreateDelegate(
                    typeof(Func<TClient, ReceivedMessageInfo, Task<ByteBuffer>>), this, methodInfo);

                using var enumerator = routeAttributes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var routeAttribute = enumerator.Current;

                    void Handler(TClient client, ReceivedMessageInfo messageInfo)
                    {
                        func(client, messageInfo).Await(
                            response =>
                            {
                                if (response == null)
                                    return;
                                client.Send((ushort)routeAttribute.MessageId, response, true,
                                    messageInfo.RequestId);
                                ByteBufferPool.Shared.Return(response);
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
        _millisecondsPerFrame = 1000 / _settings.TargetFPS;

        OnInit();
    }

    private void Update()
    {
        try
        {
            _networkListener.AddCommunicators();
            _networkListener.HandleMessages();

            _millisecondsPassed = TimeUtils.TimeSinceAppStart - _startTimeMs;

            OnUpdate();
            while (_millisecondsPassed - _frameCount * _millisecondsPerFrame >= _millisecondsPerFrame)
            {
                ++_frameCount;
                OnFixedUpdate();
                CheckHeartBeat();
                FixedUpdateClients();

                SyncPrometheus();

                var deltaTime = (TimeUtils.TimeSinceAppStart - _lastFrameTimeMs) / 1000f;
                SystemMetrics.FPS = 1f / deltaTime;
                _prometheusService.UpdateFPS(SystemMetrics.FPS);
                _lastFrameTimeMs = TimeUtils.TimeSinceAppStart;
            }

            SystemMetrics.RemainMessageCount = 0;
            _networkListener.CloseCommunicators();
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

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnFixedUpdate()
    {
    }

    protected virtual void OnDeinit()
    {
    }

    protected virtual void OnClientConnected(TClient client)
    {
        
    }
    
    protected virtual void OnClientDisconnected(TClient client)
    {
        
    }

    #endregion
    
    private void FixedUpdateClients()
    {
        foreach (var communicator in ClientList.Keys)
        {
            var client = ClientList[communicator];
            client.FixedUpdate();
        }
    }

    #region - Heart Beat -

    [MessageRoute((ushort)MessageId.HeartBeat)]
    public void OnReceivedPing(TClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        client.LastPingTime = TimeUtils.GetTimeStamp();
        client.Send(1, Array.Empty<byte>());
    }

    private void CheckHeartBeat()
    {
        var currentTime = TimeUtils.GetTimeStamp();
        foreach (var communicator in ClientList.Keys)
        {
            var client = ClientList[communicator];

            if (currentTime - client.LastPingTime >= _settings.HeartBeatInterval)
            {
                Log.Info($"{communicator.Socket.RemoteEndPoint} Heart Beat Time Out!");
                Close(communicator);
            }
        }
    }

    #endregion

    #region - Other -

    public void BroadcastMessage(ushort messageId, byte[] message)
    {
        foreach (var communicator in ClientList.Keys)
        {
            communicator.Send(messageId, message);
        }
    }

    public void Close(NetworkCommunicator communicator)
    {
        _networkListener.Close(communicator);
    }

    #endregion

    #region - Metrics -

    private void SyncPrometheus()
    {
        if (TimeUtils.TimeSinceAppStart - _lastSyncPrometheusTimeMs < SyncPrometheusInterval) return;
        _lastSyncPrometheusTimeMs = TimeUtils.TimeSinceAppStart;

        _prometheusService.UpdateCommunicatorCount(_networkListener.ConnectionCount);
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