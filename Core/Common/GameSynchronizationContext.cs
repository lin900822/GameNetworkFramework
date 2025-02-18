using System.Collections.Concurrent;

namespace Core.Common;

public class GameSynchronizationContext : SynchronizationContext
{
    private struct ActionPack
    {
        public SendOrPostCallback Callback;
        public object State;
    }
    
    private readonly ConcurrentQueue<ActionPack> _queue = new ConcurrentQueue<ActionPack>();

    public override void Post(SendOrPostCallback d, object state)
    {
        _queue.Enqueue(new ActionPack()
        {
            Callback = d,
            State = state,
        });
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue(new ActionPack()
        {
            Callback = d,
            State = state,
        });
    }

    public void ProcessQueue()
    {
        while (_queue.TryDequeue(out var actionPack))
        {
            actionPack.Callback.Invoke(actionPack.State);
        }
    }
}