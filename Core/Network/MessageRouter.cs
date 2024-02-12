using System.Buffers;
using System.Collections.Concurrent;
using Core.Logger;
using Core.Metrics;

namespace Core.Network;

public class MessageRouter
{
    private Dictionary<uint, Action<ReceivedMessageInfo>> _routeTable;

    private ConcurrentQueue<ReceivedMessageInfo> _messageInfoQueue;

    public MessageRouter()
    {
        _routeTable       = new Dictionary<uint, Action<ReceivedMessageInfo>>();
        _messageInfoQueue = new ConcurrentQueue<ReceivedMessageInfo>();
    }

    public void RegisterMessageHandler(ushort messageId, Action<ReceivedMessageInfo> handler)
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

    public void UnregisterMessageHandler(ushort messageId, Action<ReceivedMessageInfo> handler)
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
            
            if (!_messageInfoQueue.TryDequeue(out var messageInfo))
            {
                break;
            }

            if (_routeTable.TryGetValue(messageInfo.MessageId, out var handler))
            {
                handler?.Invoke(messageInfo);
            }
            else
            {
                Log.Warn($"Message Router: Received Unregistered Message, messageId = {messageInfo.MessageId}");
            }
        
            messageInfo.Release();
            SystemMetrics.HandledMessageCount++;
        }
        
        SystemMetrics.RemainMessageCount = _messageInfoQueue.Count;
    }
}