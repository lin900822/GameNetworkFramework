using System.Buffers;
using System.Collections.Concurrent;
using Google.Protobuf;
using Log;

namespace Network;

public struct MessagePack
{
    public NetworkSession Session;
    public UInt16         MessageId;
    public int            MessageLength;
    public byte[]         Message;

    public bool TryDecode<T>(out T outMessage) where T : IMessage, new()
    {
        try
        {
            outMessage = new T();
            outMessage.MergeFrom(Message, 0, MessageLength);
            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
            outMessage = default(T);
            return false;
        }
    }
}

public class MessageRouter
{
    private Dictionary<UInt16, Action<MessagePack>> _routeTable;

    private ConcurrentQueue<MessagePack> _packQueue;

    public MessageRouter()
    {
        _routeTable = new Dictionary<UInt16, Action<MessagePack>>();
        _packQueue  = new ConcurrentQueue<MessagePack>();
    }

    public void RegisterMessageHandler(UInt16 messageId, Action<MessagePack> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers += handler;
        }
        else
        {
            _routeTable[messageId] = handler;
        }
    }

    public void UnregisterMessageHandler(UInt16 messageId, Action<MessagePack> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers -= handler;
        }
    }

    public void ReceiveMessage(MessagePack messagePack)
    {
        _packQueue.Enqueue(messagePack);
    }

    public void OnUpdateLogic()
    {
        if (!_packQueue.TryDequeue(out var pack))
        {
            return;
        }

        if (_routeTable.TryGetValue(pack.MessageId, out var handler))
        {
            handler?.Invoke(pack);
            ArrayPool<byte>.Shared.Return(pack.Message);
        }
        else
        {
            Logger.Warn($"Message Router: Received Unregistered Message, messageId = {pack.MessageId}");
        }
    }

    public void Debug()
    {
        Logger.Debug($"MessageTaskQueue {_packQueue.Count}");
    }
}