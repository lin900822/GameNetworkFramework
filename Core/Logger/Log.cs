namespace Core.Logger;

public static class Log
{
    private static LogType _logLevel = LogType.Error;
    
    private static ILog _logger;

    private static ILog _instance
    {
        get
        {
            if (_logger == null) _logger = new ConsoleLog();

            return _logger;
        }
    }

    public static void Debug(string message)
    {
        if (_logLevel < LogType.Debug) return;
        _instance.Debug(message);
    }

    public static void Info(string message)
    {
        if (_logLevel < LogType.Info) return;
        _instance.Info(message);
    }

    public static void Warn(string message)
    {
        if (_logLevel < LogType.Warn) return;
        _instance.Warn(message);
    }

    public static void Error(string message)
    {
        if (_logLevel < LogType.Error) return;
        _instance.Error(message);
    }
}