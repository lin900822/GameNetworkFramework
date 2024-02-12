using System.Buffers;
using System.Collections.Concurrent;
using Core.Metrics;
using Google.Protobuf;

namespace Core.Network;

public struct ReceivedMessageInfo
{
    /// <summary>
    /// 僅伺服器使用
    /// </summary>
    public NetworkCommunicator Communicator;

    public NetworkSession Session
    {
        get
        {
            if (Communicator == null) return null;
            if (Communicator is NetworkSession session) return session;
            return null;
        }
    }

    /// <summary>
    /// 訊息Id
    /// </summary>
    public uint MessageId;

    /// <summary>
    /// 狀態碼，預設0為成功
    /// </summary>
    //public uint StateCode;

    public bool IsRequest;
    
    /// <summary>
    /// 請求Id
    /// </summary>
    public ushort RequestId;

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
            Log.Log.Error(e.ToString());
            outMessage = default(T);
            return false;
        }
    }

    public void Allocate(int size)
    {
        Message = ArrayPool<byte>.Shared.Rent(size);
    }

    public void Release()
    {
        ArrayPool<byte>.Shared.Return(Message);
    }
}

public class MessageRouter
{
    private Dictionary<uint, Action<ReceivedMessageInfo>> _routeTable;

    private ConcurrentQueue<ReceivedMessageInfo> _messageInfoQueue;

    public MessageRouter()
    {
        _routeTable       = new Dictionary<uint, Action<ReceivedMessageInfo>>();
        _messageInfoQueue = new ConcurrentQueue<ReceivedMessageInfo>();
    }

    public void RegisterMessageHandler(uint messageId, Action<ReceivedMessageInfo> handler)
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

    public void UnregisterMessageHandler(uint messageId, Action<ReceivedMessageInfo> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers -= handler;
        }
    }

    public void ReceiveMessage(ReceivedMessageInfo receivedMessageInfo)
    {
        _messageInfoQueue.Enqueue(receivedMessageInfo);
    }

    public void OnUpdateLogic()
    {
        // 每次處理500個Message
        for (var i = 0; i < 500; i++)
        {
            if (_messageInfoQueue.Count <= 0) break;
            
            if (!_messageInfoQueue.TryDequeue(out var pack))
            {
                break;
            }

            if (_routeTable.TryGetValue(pack.MessageId, out var handler))
            {
                handler?.Invoke(pack);
            }
            else
            {
                Log.Log.Warn($"Message Router: Received Unregistered Message, messageId = {pack.MessageId}");
            }
        
            pack.Release();
            SystemMetrics.HandledMessageCount++;
        }
        
        SystemMetrics.RemainMessageCount = _messageInfoQueue.Count;
    }
}