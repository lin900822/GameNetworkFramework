using System.Collections.Concurrent;
using System.Net.Sockets;
using Core.Common;
using Core.Logger;
using Core.Network;

namespace Client;

public class RequestInfo : IPoolable
{
    public ReceivedMessageInfo ReceivedMessageInfo;
    public uint MessageId;
    public ushort RequestId;
    public Action<ReceivedMessageInfo> OnCompleted;
    public Action OnTimeOut;
    public long RequestTime;
    public bool IsCompleted;

    public void Reset()
    {
        ReceivedMessageInfo = default;
        MessageId = 0;
        RequestId = 0;
        OnCompleted = null;
        OnTimeOut = null;
        RequestTime = 0;
        IsCompleted = false;
    }
}

/// <summary>
/// 實作發Request, 斷線重連
/// </summary>
public class NetworkClient
{
    private static readonly long REQUEST_TIME_OUT_MILLISECONDS = 10 * 1000;
    private static readonly long CHECK_REQUEST_TIME_OUT_MILLISECONDS = 1 * 1000;

    private MessageRouter _messageRouter;
    private NetworkConnector _connector;

    private LinkedList<RequestInfo> _requestPacks;
    private ConcurrentQueue<RequestInfo> _responseQueue;
    private Queue<RequestInfo> _timeOutRequests;

    private ConcurrentPool<RequestInfo> _requestPool;

    private ushort _requestSerialId = 0;

    private long _lastCheckRequestTimeOutTime;

    private const int _checkReconnectIntervalMs = 10_000;

    private long _lastCheckReconnectTime;
    private string _cacheIp;
    private int _cachePort;

    public NetworkClient()
    {
        _messageRouter = new MessageRouter();
        _requestPacks = new LinkedList<RequestInfo>();
        _responseQueue = new ConcurrentQueue<RequestInfo>();
        _timeOutRequests = new Queue<RequestInfo>();

        _requestPool = new ConcurrentPool<RequestInfo>();
    }

    public void Update()
    {
        _connector.Update();
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
        if (TimeUtils.TimeSinceAppStart - _lastCheckRequestTimeOutTime <
            CHECK_REQUEST_TIME_OUT_MILLISECONDS) return;

        _lastCheckRequestTimeOutTime = TimeUtils.TimeSinceAppStart;

        _timeOutRequests.Clear();

        lock (_requestPacks)
        {
            var enumerator = _requestPacks.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (TimeUtils.TimeSinceAppStart - current.RequestTime >= REQUEST_TIME_OUT_MILLISECONDS)
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
        if (TimeUtils.GetTimeStamp() - _lastCheckReconnectTime < _checkReconnectIntervalMs) return;

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
        _cacheIp = ip;
        _cachePort = port;
        _lastCheckReconnectTime = TimeUtils.GetTimeStamp();

        _connector = new NetworkConnector();
        _connector.OnReceivedMessage += OnReceivedMessage;
        _connector.OnClosed += OnClosed;
        
        _connector.Connect(ip, port);
    }
    
    private void OnReceivedMessage(ReceivedMessageInfo receivedMessageInfo)
    {
        if (receivedMessageInfo.IsRequest)
        {
            lock (_requestPacks)
            {
                if (TryGetRequestInfo(receivedMessageInfo.RequestId, out var requestPack))
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
    
    private void OnClosed(Socket socket)
    {
        _requestPacks.Clear();
        _responseQueue.Clear();
        _timeOutRequests.Clear();
        
        _connector.OnReceivedMessage -= OnReceivedMessage;
        _connector.OnClosed -= OnClosed;
        
        _lastCheckReconnectTime = TimeUtils.GetTimeStamp();
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

    #region - Public Methods -
    
    public void RegisterMessageHandler(ushort messageId, Action<ReceivedMessageInfo> handler)
    {
        _messageRouter.RegisterMessageHandler(messageId, handler);
    }

    public void UnregisterMessageHandler(ushort messageId, Action<ReceivedMessageInfo> handler)
    {
        _messageRouter.UnregisterMessageHandler(messageId, handler);
    }

    public void SendMessage(ushort messageId, byte[] message)
    {
        if (_connector.ConnectState != ConnectState.Connected) return;
        _connector.Send(messageId, message);
    }

    public void SendRequest(ushort messageId, byte[] request, Action<ReceivedMessageInfo> onCompleted,
        Action onTimeOut = null)
    {
        if (_connector.ConnectState != ConnectState.Connected) return;

        lock (this)
        {
            ++_requestSerialId;
        }

        var requestPack = _requestPool.Rent();
        requestPack.MessageId = messageId;
        requestPack.RequestId = _requestSerialId;
        requestPack.OnCompleted = onCompleted;
        requestPack.OnTimeOut = onTimeOut;
        requestPack.RequestTime = TimeUtils.TimeSinceAppStart;

        lock (_requestPacks)
        {
            _requestPacks.AddLast(requestPack);
        }

        _connector.Send(messageId, request, true, _requestSerialId);
    }

    public Task<ReceivedMessageInfo> SendRequest(ushort messageId, byte[] request, Action onTimeOut = null)
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
    
    #endregion
}