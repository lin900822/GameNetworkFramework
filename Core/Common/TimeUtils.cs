using System.Diagnostics;

namespace Core.Common;

public static class TimeUtils
{
    private static Stopwatch _stopwatch;

    static TimeUtils()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// 啟動到現在經過的毫秒數
    /// </summary>
    public static long MilliSecondsSinceStart => _stopwatch.ElapsedMilliseconds;
    
    public static long GetTimeStamp()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(timeSpan.TotalSeconds);
    }
}