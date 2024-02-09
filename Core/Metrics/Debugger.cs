using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Core.Metrics;

public class Debugger
{
    private int _logIntervalMilliSeconds = 1000;
    private Action _onInterval;
    private Timer _timer;
    private Stopwatch _stopwatch;

    public Debugger()
    {
        _stopwatch = new Stopwatch();
    }

    public void Start(int interval, Action action)
    {
        _logIntervalMilliSeconds = interval;
        _onInterval = action;
        
        _timer = new Timer(_logIntervalMilliSeconds);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }
    
    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        _onInterval?.Invoke();
    }
}