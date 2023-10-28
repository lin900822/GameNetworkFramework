using System.Collections.Concurrent;
using Log;
using Network;

namespace Common;

public class MessageRouter
{
    private struct MessageTask
    {
        public NetworkSession Session;
        public UInt16    MessageId;
        public byte[]    Message;
    }

    private Dictionary<UInt16, Action<NetworkSession, byte[]>> _routeTable;

    private ConcurrentQueue<MessageTask> _taskQueue;

    public MessageRouter()
    {
        _routeTable = new Dictionary<UInt16, Action<NetworkSession, byte[]>>();
        _taskQueue  = new ConcurrentQueue<MessageTask>();
    }

    public void RegisterMessageHandler(UInt16 messageId, Action<NetworkSession, byte[]> handler)
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

    public void UnregisterMessageHandler(UInt16 messageId, Action<NetworkSession, byte[]> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers -= handler;
        }
    }

    public void ReceiveMessage(NetworkSession session, UInt16 messageId, byte[] message)
    {
        var task = new MessageTask()
        {
            Session    = session,
            MessageId = messageId,
            Message   = message,
        };

        _taskQueue.Enqueue(task);
    }

    public void OnUpdateLogic()
    {
        if (!_taskQueue.TryDequeue(out var task))
        {
            return;
        }

        if (_routeTable.TryGetValue(task.MessageId, out var handler))
        {
            handler?.Invoke(task.Session, task.Message);
        }
        else
        {
            Logger.Warn($"Message Router: Received Unregistered Message, messageId = {task.MessageId}");
        }
    }

    public void Debug()
    {
        Logger.Debug($"MessageTaskQueue {_taskQueue.Count}");
    }
}