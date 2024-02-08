using System.Diagnostics;

namespace Core.Metrics;

public class Debugger
{
    private int _logIntervalMilliSeconds = 1000;
    private Action _onInterval;
    private Stopwatch _stopwatch;

    public Debugger()
    {
        _stopwatch = new Stopwatch();
    }

    public void Start(int interval, Action action)
    {
        _logIntervalMilliSeconds = interval;
        _onInterval = action;
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    public void Update()
    {
        if (_stopwatch.ElapsedMilliseconds >= _logIntervalMilliSeconds)
        {
            _stopwatch.Restart();
            _onInterval?.Invoke();
        }
    }
}