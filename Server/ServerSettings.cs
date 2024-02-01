namespace Server;

public class ServerSettings
{
    public uint   ServerId;
    public string ServerName;

    public int Port = 10001;
    public int MaxSessionCount = 1;
    public int HeartBeat       = 120;
}