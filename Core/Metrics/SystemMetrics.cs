namespace Core.Metrics;

public static class SystemMetrics
{
    private static int _receivedMessageCount = 0;
    private static int _sessionCount = 0;

    public static void AddReceivedMessageCount()
    {
        Interlocked.Increment(ref _receivedMessageCount);
    }

    public static void UpdateSessionCount(int sessionCount)
    {
        _sessionCount = sessionCount;
    }
}