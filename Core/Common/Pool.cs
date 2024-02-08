using System.Collections.Concurrent;

namespace Core.Common;

public interface IPoolable
{
    void Reset();
}

public class Pool<T> where T : IPoolable, new()
{
    private Queue<T> _queue;

    public Pool()
    {
        _queue = new Queue<T>();
    }

    public T Rent()
    {
        return _queue.TryDequeue(out var result) ? result : new T();
    }

    public void Return(T item)
    {
        item.Reset();
        _queue.Enqueue(item);
    }
}

public class ConcurrentPool<T> where T : IPoolable, new()
{
    private ConcurrentQueue<T> _queue;

    public ConcurrentPool()
    {
        _queue = new ConcurrentQueue<T>();
    }

    public T Rent()
    {
        return _queue.TryDequeue(out var result) ? result : new T();
    }

    public void Return(T item)
    {
        item.Reset();
        _queue.Enqueue(item);
    }
}