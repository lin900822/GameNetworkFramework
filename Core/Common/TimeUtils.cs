using System.Diagnostics;

namespace Core.Common;

public static class TimeUtils
{
    private static readonly long AppStartTime;
    private static readonly DateTime UnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

    static TimeUtils()
    {
        AppStartTime = GetTimeStamp();
    }

    /// <summary>
    /// 啟動到現在經過的毫秒數
    /// </summary>
    public static long TimeSinceAppStart => GetTimeStamp() - AppStartTime;
    
    /// <summary>
    /// 從 1970-01-01 00:00:00 到現在的毫秒數
    /// </summary>
    public static long GetTimeStamp()
    {
        TimeSpan timeSpan = DateTime.UtcNow - UnixTime;
        return Convert.ToInt64(timeSpan.TotalMilliseconds);
    }
}