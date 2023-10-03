namespace Log;

public class Logger
{
    private static ILog _logger;

    private static ILog _instance
    {
        get
        {
            if (_logger == null) _logger = new ConsoleLog();

            return _logger;
        }
    }

    public static void LogDebug(string message)
    {
        _instance.LogDebug(message);
    }

    public static void LogInfo(string message)
    {
        _instance.LogInfo(message);
    }

    public static void LogWarning(string message)
    {
        _instance.LogWarning(message);
    }

    public static void LogError(string message)
    {
        _instance.LogError(message);
    }
}