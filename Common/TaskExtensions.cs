namespace Common;

public static class TaskExtensions
{
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