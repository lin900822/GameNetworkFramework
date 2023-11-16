namespace Server;

public abstract class ClientBase
{
    public long LastPingTime { get; set; }
    
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
    
    protected virtual void OnInit()   {}
    protected virtual void OnUpdate() {}
    protected virtual void OnDeinit() {}
}