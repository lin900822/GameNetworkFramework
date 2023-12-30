using System.Collections.Concurrent;

namespace Server;

public class ServerSynchronizationContext : SynchronizationContext
{
    private readonly ConcurrentStack<Action> _stack = new ConcurrentStack<Action>();

    public override void Post(SendOrPostCallback d, object state)
    {
        _stack.Push(() => d(state));
    }

    public void ProcessQueue()
    {
        while (_stack.TryPop(out var action))
        {
            action();
        }
    }
}