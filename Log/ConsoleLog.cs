namespace Log;

public class ConsoleLog : ILog
{
    public void Debug(string message)
    {
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Debug] {message}");
        }
    }

    public void Info(string message)
    {
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Info] {message}");
        }
    }

    public void Warn(string message)
    {
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Warning] {message}");
        }
    }

    public void Error(string message)
    {
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Error] {message}");
        }
    }
}