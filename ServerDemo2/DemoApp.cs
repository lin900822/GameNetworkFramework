using Shared.Common;

namespace ServerDemo2;

public class DemoApp : AppBase
{
    private DemoServer _demoServer;
    
    public DemoApp(DemoServer demoServer)
    {
        _demoServer = demoServer;
    }
    
    protected override void OnInit()
    {
        _demoServer.Init();
    }

    protected override void OnUpdate()
    {
        _demoServer.Update();
    }

    protected override void OnDeinit()
    {
        _demoServer.Deinit();
    }
}