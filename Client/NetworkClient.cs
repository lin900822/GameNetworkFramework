using System.Collections.Concurrent;
using System.Net.Sockets;
using Core.Common;
using Core.Log;
using Core.Network;

namespace Client;

public class RequestInfo : IPoolable
{
    public ReceivedMessageInfo         ReceivedMessageInfo;
    public uint                        MessageId;
    public uint                        RequestId;
    public Action<ReceivedMessageInfo> OnCompleted;
    public Action                      OnTimeOut;
    public long                        RequestTime;
    public bool                        IsCompleted;

    public void Reset()
    {
        ReceivedMessageInfo = default;
        MessageId           = 0;
        RequestId           = 0;
        OnCompleted         = null;
        OnTimeOut           = null;
        RequestTime         = 0;
        IsCompleted         = false;
    }
}

/// <summary>
/// 實作發Request, 斷線重連
/// </summary>
public class NetworkClient
{
    private static readonly long REQUEST_TIME_OUT_MILLISECONDS       = 5 * 1000;
    private static readonly long CHECK_REQUEST_TIME_OUT_MILLISECONDS = 1 * 1000;

    private MessageRouter    _messageRouter;
    private NetworkConnector _connector;

    private LinkedList<RequestInfo>      _requestPacks;
    private ConcurrentQueue<RequestInfo> _responseQueue;
    private Queue<RequestInfo>           _timeOutRequests;

    private ConcurrentPool<RequestInfo> _requestPool;

    private uint _requestSerialId = (uint)(int.MaxValue) + 1;

    private long _lastCheckRequestTimeOutTime;

    private const int _checkReconnectSeconds = 10;

    private long   _lastCheckReconnectTime;
    private string _cacheIp;
    private int    _cachePort;

    public NetworkClient()
    {
        _messageRouter = new MessageRouter();
        _connector     = new NetworkConnector();

        _requestPacks    = new LinkedList<RequestInfo>();
        _responseQueue   = new ConcurrentQueue<RequestInfo>();
        _timeOutRequests = new Queue<RequestInfo>();

        _requestPool = new ConcurrentPool<RequestInfo>();

        _connector.OnReceivedMessage += OnReceivedMessage;
        _connector.OnClosed += OnClosed;
    }

    ~NetworkClient()
    {
        _connector.OnReceivedMessage -= OnReceivedMessage;
        _connector.OnClosed -= OnClosed;
    }

    private void OnClosed(Socket socket)
    {
        _lastCheckReconnectTime = TimeUtils.GetTimeStamp();
    }

    private void OnReceivedMessage(ReceivedMessageInfo receivedMessageInfo)
    {
        // 普通的Message: MessageId = 0 ~ int.MaxValue
        // 請求 Request: MessageId = int.MaxValue ~ uint.MaxValue
        if (receivedMessageInfo.MessageId >= (uint)(int.MaxValue) + 1)
        {
            lock (_requestPacks)
            {
                if (TryGetRequestInfo(receivedMessageInfo.MessageId, out var requestPack))
                {
                    requestPack.ReceivedMessageInfo = receivedMessageInfo;
                    _responseQueue.Enqueue(requestPack);
                }
            }
        }
        else
        {
            _messageRouter.ReceiveMessage(receivedMessageInfo);
        }
    }

    private bool TryGetRequestInfo(uint requestId, out RequestInfo outRequestInfo)
    {
        var enumerator = _requestPacks.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (current.RequestId != requestId) continue;

            outRequestInfo = current;
            return true;
        }

        outRequestInfo = default;
        return false;
    }

    public void Update()
    {
        _messageRouter.OnUpdateLogic();

        if (_responseQueue.TryDequeue(out var requestPack))
        {
            requestPack.OnCompleted?.Invoke(requestPack.ReceivedMessageInfo);
            requestPack.IsCompleted = true;
            requestPack.ReceivedMessageInfo.Release();
        }

        CheckRequestTimeOut();
        CheckReconnect();
    }

    private void CheckRequestTimeOut()
    {
        if (TimeUtils.MilliSecondsSinceStart - _lastCheckRequestTimeOutTime <
            CHECK_REQUEST_TIME_OUT_MILLISECONDS) return;

        _lastCheckRequestTimeOutTime = TimeUtils.MilliSecondsSinceStart;

        _timeOutRequests.Clear();

        lock (_requestPacks)
        {
            var enumerator = _requestPacks.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (TimeUtils.MilliSecondsSinceStart - current.RequestTime >= REQUEST_TIME_OUT_MILLISECONDS)
                {
                    _timeOutRequests.Enqueue(current);
                }
            }

            foreach (var request in _timeOutRequests)
            {
                _requestPacks.Remove(request);
            }
        }

        foreach (var request in _timeOutRequests)
        {
            if (!request.IsCompleted)
            {
                request.OnTimeOut?.Invoke();
            }

            _requestPool.Return(request);
        }
    }

    private void CheckReconnect()
    {
        if (TimeUtils.GetTimeStamp() - _lastCheckReconnectTime < _checkReconnectSeconds) return;
        
        _lastCheckReconnectTime = TimeUtils.GetTimeStamp();
        Reconnect();
    }

    private void Reconnect()
    {
        if (_connector.ConnectState != ConnectState.Disconnected) return;

        Connect(_cacheIp, _cachePort);
    }

    public void Connect(string ip, int port)
    {
        _cacheIp                = ip;
        _cachePort              = port;
        _lastCheckReconnectTime = TimeUtils.GetTimeStamp();
        
        _connector.Connect(ip, port);
    }

    public void RegisterMessageHandler(uint messageId, Action<ReceivedMessageInfo> handler)
    {
        _messageRouter.RegisterMessageHandler(messageId, handler);
    }

    public void UnregisterMessageHandler(uint messageId, Action<ReceivedMessageInfo> handler)
    {
        _messageRouter.UnregisterMessageHandler(messageId, handler);
    }

    public void SendMessage(uint messageId, byte[] message)
    {
        if (_connector.ConnectState != ConnectState.Connected) return;
        _connector.Send(messageId, message);
    }

    public void SendRequest(uint messageId, byte[] request, Action<ReceivedMessageInfo> onCompleted,
        Action                   onTimeOut = null)
    {
        if (_connector.ConnectState != ConnectState.Connected) return;

        var requestId = Interlocked.Increment(ref _requestSerialId);

        var requestPack = _requestPool.Rent();
        requestPack.MessageId   = messageId;
        requestPack.RequestId   = requestId;
        requestPack.OnCompleted = onCompleted;
        requestPack.OnTimeOut   = onTimeOut;
        requestPack.RequestTime = TimeUtils.MilliSecondsSinceStart;

        lock (_requestPacks)
        {
            _requestPacks.AddLast(requestPack);
        }

        _connector.Send(messageId, request, requestId);
    }

    public Task<ReceivedMessageInfo> SendRequest(uint messageId, byte[] request, Action onTimeOut = null)
    {
        var taskCompletionSource =
            new TaskCompletionSource<ReceivedMessageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            SendRequest(messageId, request,
                (info) =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(info);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                },
                onTimeOut);
        }
        catch (Exception ex)
        {
            taskCompletionSource.SetException(ex);
        }

        return taskCompletionSource.Task;
    }
}