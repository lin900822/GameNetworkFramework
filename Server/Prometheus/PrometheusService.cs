using Prometheus;

namespace Server.Prometheus;

public class PrometheusService
{
    private Gauge _handledMessagePerSecondGauge =
        Metrics.CreateGauge("handled_messages_per_second", "Messages Handles per Second");

    private Gauge _remainMessageCountGauge = Metrics.CreateGauge("remain_message_count", "Remain Message Count");
    private Gauge _sessionCountGauge = Metrics.CreateGauge("session_count", "Session Count");
    private Gauge _fpsGauge = Metrics.CreateGauge("fps", "FPS");
    
    private Gauge _memoryGauge = Metrics.CreateGauge("memory", "Memory Usage");
    private Gauge _gc0Gauge = Metrics.CreateGauge("gc0_per_second", "GC0 per Second");
    private Gauge _gc1Gauge = Metrics.CreateGauge("gc1_per_second", "GC1 per Second");
    private Gauge _gc2Gauge = Metrics.CreateGauge("gc2_per_second", "GC2 per Second");

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

    public void UpdateRemainMessageCount(int value)
    {
        _remainMessageCountGauge.Set(value);
    }

    public void UpdateCommunicatorCount(int value)
    {
        _sessionCountGauge.Set(value);
    }

    public void UpdateFPS(float value)
    {
        _fpsGauge.Set(value);
    }
    
    public void UpdateMemory(float value)
    {
        _memoryGauge.Set(value);
    }
    
    public void UpdateGC0PerSecond(int value)
    {
        _gc0Gauge.Set(value);
    }
    
    public void UpdateGC1PerSecond(int value)
    {
        _gc1Gauge.Set(value);
    }

    public void UpdateGC2PerSecond(int value)
    {
        _gc2Gauge.Set(value);
    }
}