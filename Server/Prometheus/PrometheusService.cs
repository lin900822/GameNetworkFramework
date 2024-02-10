using Prometheus;

namespace Server.Prometheus;

public class PrometheusService
{
    private Gauge _handledMessagePerSecondGauge = Metrics.CreateGauge("handled_messages_per_second", "Messages Handles per Second");
    private Gauge _sessionCountGauge = Metrics.CreateGauge("session_count", "Session Count");
    
    private MetricServer _metricServer;
    private const int _port = 19001;
    
    public void Start()
    {
        // 初始化 Prometheus 库
        Metrics.SuppressDefaultMetrics();
        
        // 这里可以添加其他初始化代码

        // 启动 Web 服务器，用于暴露 Prometheus metrics
        _metricServer = new MetricServer(port: _port);
        _metricServer.Start();
    }

    public void Stop()
    {
        if (_metricServer == null) return;
        _metricServer.Stop();
    }

    public void UpdateHandledMessagePerSecond(int value)
    {
        _handledMessagePerSecondGauge.Set(value);
    }

    public void UpdateSessionCount(int value)
    {
        _sessionCountGauge.Set(value);
    }
}