using System.Buffers;
using System.Collections.Concurrent;
using Core.Logger;
using Core.Metrics;

namespace Core.Network;

public class MessageRouter
{
    private Dictionary<uint, Action<ReceivedMessageInfo>> _routeTable;

    public MessageRouter()
    {
        _routeTable       = new Dictionary<uint, Action<ReceivedMessageInfo>>();
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
        if (_routeTable.TryGetValue(receivedMessageInfo.MessageId, out var handler))
        {
            handler?.Invoke(receivedMessageInfo);
        }
        else
        {
            Log.Warn($"Message Router: Received Unregistered Message, messageId = {receivedMessageInfo.MessageId}");
        }
        
        receivedMessageInfo.Release();
        SystemMetrics.HandledMessageCount++;
    }
}