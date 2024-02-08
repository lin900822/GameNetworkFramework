namespace Core.Metrics;

public static class SystemMetrics
{
    public static int HandledMessageCount { get; set; }
    public static int HandledFrame { get; set; }
    public static int RemainMessageCount { get; set; }
    public static int SessionCount { get; private set; }
    
    public static void UpdateSessionCount(int sessionCount)
    {
        SessionCount = sessionCount;
    }

    public static void ResetSessionCount()
    {
        SessionCount = 0;
    }
}