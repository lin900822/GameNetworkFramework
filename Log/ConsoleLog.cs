namespace Log;

public class ConsoleLog : ILog
{
    public void Debug(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Debug] ");
        Console.WriteLine(message);
    }

    public void Info(string  message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Info] ");
        Console.WriteLine(message);
    }

    public void Warn(string  message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Warning] ");
        Console.WriteLine(message);
    }

    public void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Error] ");
        Console.WriteLine(message);
    }
}