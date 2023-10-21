using Log;
using Network;

namespace Common;

public class MessageRouter
{
    private struct MessageTask
    {
        public NetworkClient Client;
        public UInt16    MessageId;
        public byte[]    Message;
    }

    private Dictionary<UInt16, Action<NetworkClient, byte[]>> _routeTable;

    private Queue<MessageTask> _taskQueue;

    public MessageRouter()
    {
        _routeTable = new Dictionary<UInt16, Action<NetworkClient, byte[]>>();
        _taskQueue  = new Queue<MessageTask>();
    }

    public void RegisterMessageHandler(UInt16 messageId, Action<NetworkClient, byte[]> handler)
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

    public void UnregisterMessageHandler(UInt16 messageId, Action<NetworkClient, byte[]> handler)
    {
        if (_routeTable.TryGetValue(messageId, out var handlers))
        {
            handlers -= handler;
        }
    }

    public void ReceiveMessage(NetworkClient client, UInt16 messageId, byte[] message)
    {
        var task = new MessageTask()
        {
            Client    = client,
            MessageId = messageId,
            Message   = message,
        };

        lock (_taskQueue)
        {
            _taskQueue.Enqueue(task);
        }
    }

    public void OnUpdateLogic()
    {
        MessageTask task;
        lock (_taskQueue)
        {
            if (_taskQueue.Count > 0)
            {
                task = _taskQueue.Dequeue();
            }
            else
            {
                return;
            }
        }

        if (_routeTable.TryGetValue(task.MessageId, out var handler))
        {
            handler?.Invoke(task.Client, task.Message);
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