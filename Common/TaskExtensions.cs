using System.Runtime.CompilerServices;

namespace Common;

public static class TaskExtensions
{
    /// <summary>
    /// 在MainThread上安全等待(只能在MainThread上使用)
    /// </summary>
    public static void SafeWait(this Task task)
    {
        TaskAwaiter awaiter = task.GetAwaiter();
        GameSynchronizationContext synchronizationContext = SynchronizationContext.Current as GameSynchronizationContext;
        if (synchronizationContext != null)
        {
            while (!awaiter.IsCompleted)
            {
                Thread.Sleep(0);
                synchronizationContext.ProcessQueue();
            }
        }

        awaiter.GetResult();
    }
    
    /// <summary>
    /// 在MainThread上安全等待(只能在MainThread上使用)
    /// </summary>
    public static T SafeWait<T>(this Task<T> task)
    {
        TaskAwaiter<T> awaiter = task.GetAwaiter();
        GameSynchronizationContext synchronizationContext = SynchronizationContext.Current as GameSynchronizationContext;
        if (synchronizationContext != null)
        {
            while (!awaiter.IsCompleted)
            {
                Thread.Sleep(0);
                synchronizationContext.ProcessQueue();
            }
        }

        return awaiter.GetResult();
    }
    
    /// <summary>
    /// 一發即忘的安全同步呼叫非同步方法
    /// </summary>
    public static async void Await(this Task task, Action? onCompleted = null, Action<Exception>? onError = null)
    {
        try
        {
            await task;
            onCompleted?.Invoke();
        }
        catch (Exception e)
        {
            onError?.Invoke(e);
        }
    }
}