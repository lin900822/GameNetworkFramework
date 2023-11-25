using System.Buffers;
using System.Collections.Concurrent;
using Network;

namespace Client;

public struct RequestPack
{
    public Packet         Packet;
    public ushort              RequestMessageId;
    public Action<Packet> OnCompleted;
    public Action              OnTimeOut;
}

public class ClientBase
{
    private static readonly int REQUEST_TIME_OUT_SECONDS = 5;

    private MessageRouter    _messageRouter;
    private NetworkConnector _connector;

    private LinkedList<RequestPack>      _requestPacks;
    private ConcurrentQueue<RequestPack> _requestQueue;

    public ClientBase()
    {
        _messageRouter = new MessageRouter();
        _connector     = new NetworkConnector();

        _requestPacks = new LinkedList<RequestPack>();
        _requestQueue = new ConcurrentQueue<RequestPack>();

        _connector.OnReceivedMessage += OnReceivedMessage;
    }

    private void OnReceivedMessage(Packet packet)
    {
        lock (_requestPacks)
        {
            if (TryGetRequestPack(packet.MessageId, out var requestPack))
            {
                requestPack.Packet = packet;
                _requestQueue.Enqueue(requestPack);
                _requestPacks.Remove(requestPack);
                return;
            }
        }

        _messageRouter.ReceiveMessage(packet);
    }

    private bool TryGetRequestPack(ushort responseMessageId, out RequestPack outRequestPack)
    {
        var enumerator = _requestPacks.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (current.RequestMessageId == responseMessageId)
            {
                outRequestPack = current;
                return true;
            }
        }

        outRequestPack = default;
        return false;
    }

    public void Update()
    {
        _messageRouter.OnUpdateLogic();

        if (_requestQueue.TryDequeue(out var requestPack))
        {
            requestPack.OnCompleted?.Invoke(requestPack.Packet);
            requestPack.Packet.Release();
        }
    }

    public void Connect(string ip, int port)
    {
        _connector.Connect(ip, port);
    }

    public void RegisterMessageHandler(ushort messageId, Action<Packet> handler)
    {
        _messageRouter.RegisterMessageHandler(messageId, handler);
    }

    public void UnregisterMessageHandler(ushort messageId, Action<Packet> handler)
    {
        _messageRouter.UnregisterMessageHandler(messageId, handler);
    }

    public void SendMessage(ushort messageId, byte[] message)
    {
        _connector.Send(messageId, message);
    }

    public void SendRequest(ushort requestMessageId, byte[] request, Action<Packet> onCompleted,
        Action                     onTimeOut = null)
    {
        var requestPack = new RequestPack()
        {
            RequestMessageId = requestMessageId,
            OnCompleted      = onCompleted,
            OnTimeOut        = onTimeOut,
        };

        lock (_requestPacks)
        {
            _requestPacks.AddLast(requestPack);
        }

        _connector.Send(requestMessageId, request);
    }
}