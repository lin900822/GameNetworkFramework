namespace Shared.Common;

public abstract class AppBase
{
    public void Start()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Deinit(); };
        
        var synchronizationContext = new GameSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);

        Init();
        
        while (true)
        {
            Update();
            synchronizationContext.ProcessQueue();
            Thread.Sleep(1);
        }
    }

    private void Init()
    {
        OnInit();
    }
    
    private void Update()
    {
        OnUpdate();
    }

    private void Deinit()
    {
        OnDeinit();
    }

    protected abstract void OnInit();

    protected abstract void OnUpdate();

    protected abstract void OnDeinit();
}