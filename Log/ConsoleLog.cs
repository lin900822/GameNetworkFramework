namespace Log;

public class ConsoleLog : ILog
{
    private static bool IsLogDebug = true;
    private static bool IsLogInfo = true;
    private static bool IsLogWarn = true;
    private static bool IsLogError = true;
    
    public void Debug(string message)
    {
        if (!IsLogDebug) return;
        
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Debug] {message}");
        }
    }

    public void Info(string message)
    {
        if (!IsLogInfo) return;
        
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Info] {message}");
        }
    }

    public void Warn(string message)
    {
        if (!IsLogWarn) return;
        
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Warn] {message}");
        }
    }

    public void Error(string message)
    {
        if (!IsLogError) return;
        
        lock (this)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [Error] {message}");
        }
    }
}