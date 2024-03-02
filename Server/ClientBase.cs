namespace Server;

public abstract class ClientBase<T> where T : ClientBase<T>, new()
{
    public ServerBase<T> Server { get; private set; }

    public long LastPingTime { get; set; }

    public void SetServer(ServerBase<T> server)
    {
        Server = server;
    }

    public void Init()
    {
        OnInit();
    }

    public void Update()
    {
        OnUpdate();
    }

    public void Deinit()
    {
        OnDeinit();
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnDeinit()
    {
    }
}