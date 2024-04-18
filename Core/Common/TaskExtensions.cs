using System.Runtime.CompilerServices;

namespace Core.Common;

public static class TaskExtensions
{
    /// <summary>
    /// 在MainThread上安全等待, 其他Thread維持原處理方式
    /// </summary>
    public static void SafeWait(this Task task)
    {
        TaskAwaiter awaiter = task.GetAwaiter();
        GameSynchronizationContext synchronizationContext =
            SynchronizationContext.Current as GameSynchronizationContext;
        if (synchronizationContext != null)
        {
            while (!awaiter.IsCompleted)
            {
                Thread.Sleep(1); // 避免佔走CPU所有資源
                synchronizationContext.ProcessQueue();
            }
        }

        awaiter.GetResult();
    }

    /// <summary>
    /// 在MainThread上安全等待, 其他Thread維持原處理方式
    /// </summary>
    public static T SafeWait<T>(this Task<T> task)
    {
        TaskAwaiter<T> awaiter = task.GetAwaiter();
        GameSynchronizationContext synchronizationContext =
            SynchronizationContext.Current as GameSynchronizationContext;
        if (synchronizationContext != null)
        {
            while (!awaiter.IsCompleted)
            {
                Thread.Sleep(1); // 避免佔走CPU所有資源
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

    /// <summary>
    /// 一發即忘的安全同步呼叫非同步方法
    /// </summary>
    public static async void Await<T>(this Task<T> task, Action<T>? onCompleted = null,
        Action<Exception>? onError = null)
    {
        try
        {
            T response = await task;
            onCompleted?.Invoke(response);
        }
        catch (Exception e)
        {
            onError?.Invoke(e);
        }
    }
}