using Shared.Common;

namespace SnakeMainServer;

public class SnakeMainApp : AppBase
{
    private MainServer _mainServer;

    public SnakeMainApp(MainServer mainServer)
    {
        _mainServer = mainServer;
    }

    protected override void OnInit()
    {
        _mainServer.Init();
    }

    protected override void OnUpdate()
    {
        _mainServer.Update();
    }

    protected override void OnDeinit()
    {
        _mainServer.Deinit();
    }
}