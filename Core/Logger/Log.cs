namespace Core.Logger;

public static class Log
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

    public static void Debug(string message)
    {
        _instance.Debug(message);
    }

    public static void Info(string message)
    {
        _instance.Info(message);
    }

    public static void Warn(string message)
    {
        _instance.Warn(message);
    }

    public static void Error(string message)
    {
        _instance.Error(message);
    }
}