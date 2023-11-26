using System.Collections.Concurrent;
using Common;
using Network;
using Protocol;

namespace Client;

public struct RequestInfo
{
    public ReceivedMessageInfo         ReceivedMessageInfo;
    public uint                        MessageId;
    public uint                        RequestId;
    public Action<ReceivedMessageInfo> OnCompleted;
    public Action                      OnTimeOut;
    public long                        RequestTime;
}

public class ClientBase
{
    private static readonly long REQUEST_TIME_OUT_MILLISECONDS       = 5 * 1000;
    private static readonly long CHECK_REQUEST_TIME_OUT_MILLISECONDS = 1 * 1000;

    private MessageRouter    _messageRouter;
    private NetworkConnector _connector;

    private LinkedList<RequestInfo>      _requestPacks;
    private ConcurrentQueue<RequestInfo> _responseQueue;
    private Queue<RequestInfo>           _timeOutRequests;

    private uint _requestSerialId = (uint)(int.MaxValue) + 1;

    private long _lastCheckRequestTimeOutTime;

    public ClientBase()
    {
        _messageRouter = new MessageRouter();
        _connector     = new NetworkConnector();

        _requestPacks    = new LinkedList<RequestInfo>();
        _responseQueue    = new ConcurrentQueue<RequestInfo>();
        _timeOutRequests = new Queue<RequestInfo>();

        _connector.OnReceivedMessage += OnReceivedMessage;
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
                
                    _requestPacks.Remove(requestPack);
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
            requestPack.ReceivedMessageInfo.Release();
        }

        CheckRequestTimeOut();
    }

    private void CheckRequestTimeOut()
    {
        if(TimeUtils.MilliSecondsSinceStart - _lastCheckRequestTimeOutTime < CHECK_REQUEST_TIME_OUT_MILLISECONDS) return;

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
            request.OnTimeOut?.Invoke();
        }
    }

    public void Connect(string ip, int port)
    {
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
        _connector.Send(messageId, message);
    }

    public void SendRequest(uint messageId, byte[] request, Action<ReceivedMessageInfo> onCompleted, Action onTimeOut = null)
    {
        var requestId = Interlocked.Increment(ref _requestSerialId);

        var requestPack = new RequestInfo()
        {
            MessageId   = messageId,
            RequestId   = requestId,
            OnCompleted = onCompleted,
            OnTimeOut   = onTimeOut,
            RequestTime = TimeUtils.MilliSecondsSinceStart,
        };

        lock (_requestPacks)
        {
            _requestPacks.AddLast(requestPack);
        }

        _connector.Send(messageId, request, requestId);
    }
}