namespace Core.Metrics;

public static class SystemMetrics
{
    public static float FPS                 { get; set; }
    public static int   HandledMessageCount { get; set; }
    public static int   RemainMessageCount  { get; set; }
    public static int   LastGC0                 { get; set; }
    public static int   LastGC1                 { get; set; }
    public static int   LastGC2                 { get; set; }
    
}