namespace Server;

public class ServerSettings
{
    public uint   ServerId;
    public string ServerName;

    public int Port               = 10001;
    public int MaxConnectionCount = 1;
    public int HeartBeatInterval  = 120_000;
    public int TargetFPS          = 20;

    public int PrometheusPort = 20001;
}