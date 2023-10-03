namespace Log;

public class ConsoleLog : ILog
{
    public void LogDebug(string message)
    {
        Console.Write($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Debug] ");
        Console.WriteLine(message);
    }

    public void LogInfo(string  message)
    {
        Console.Write($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Info] ");
        Console.WriteLine(message);
    }

    public void LogWarning(string  message)
    {
        Console.Write($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Warning] ");
        Console.WriteLine(message);
    }

    public void LogError(string message)
    {
        Console.Write($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Error] ");
        Console.WriteLine(message);
    }
}