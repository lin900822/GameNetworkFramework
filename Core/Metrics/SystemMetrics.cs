namespace Core.Metrics;

public static class SystemMetrics
{
    private static int _receivedMessageCount = 0;
    public static int ReceivedMessageCount => _receivedMessageCount;
    public static int SessionCount { get; private set; } = 0;

    public static void AddReceivedMessageCount()
    {
        Interlocked.Increment(ref _receivedMessageCount);
    }

    public static void ResetReceivedMessageCount()
    {
        Interlocked.Exchange(ref _receivedMessageCount, 0);
    }

    public static void UpdateSessionCount(int sessionCount)
    {
        SessionCount = sessionCount;
    }

    public static void ResetSessionCount()
    {
        SessionCount = 0;
    }
}