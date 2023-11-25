using System.Buffers;
using System.Collections.Concurrent;
using Google.Protobuf;
using Log;

namespace Network;

public struct Packet
{
    /// <summary>
    /// 僅伺服器使用
    /// </summary>
    public NetworkSession Session;

    /// <summary>
    /// 訊息Id
    /// </summary>
    public ushort MessageId;

    /// <summary>
    /// 狀態碼，預設0為成功
    /// </summary>
    public uint StateCode;

    /// <summary>
    /// 有意義的訊息長度
    /// </summary>
    public int MessageLength;

    /// <summary>
    /// 訊息本體
    /// </summary>
    public byte[] Message;

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

    public void Release()
    {
        ArrayPool<byte>.Shared.Return(Message);
    }
}

public class MessageRouter
{
    private Dictionary<ushort, Action<Packet>> _routeTable;

    private ConcurrentQueue<Packet> _packetQueue;

    public MessageRouter()
    {
        _routeTable  = new Dictionary<ushort, Action<Packet>>();
        _packetQueue = new ConcurrentQueue<Packet>();
    }

    public void RegisterMessageHandler(ushort messageId, Action<Packet> handler)
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

    public void UnregisterMessageHandler(ushort messageId, Action<Packet> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers -= handler;
        }
    }

    public void ReceiveMessage(Packet packet)
    {
        _packetQueue.Enqueue(packet);
    }

    public void OnUpdateLogic()
    {
        if (!_packetQueue.TryDequeue(out var pack))
        {
            return;
        }

        if (_routeTable.TryGetValue(pack.MessageId, out var handler))
        {
            handler?.Invoke(pack);
            pack.Release();
        }
        else
        {
            Logger.Warn($"Message Router: Received Unregistered Message, messageId = {pack.MessageId}");
        }
    }

    public void Debug()
    {
        Logger.Debug($"MessageTaskQueue {_packetQueue.Count}");
    }
}