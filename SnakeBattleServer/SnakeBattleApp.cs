using Shared.Common;

namespace SnakeBattleServer;

public class SnakeBattleApp : AppBase
{
    private BattleServer _battleServer;

    public SnakeBattleApp(BattleServer battleServer)
    {
        _battleServer = battleServer;
    }

    protected override void OnInit()
    {
        _battleServer.Init();
    }

    protected override void OnUpdate()
    {
        _battleServer.Update();
    }

    protected override void OnDeinit()
    {
        _battleServer.Deinit();
    }
}