using System.Timers;
using Log;
using Network;
using Timer = System.Timers.Timer;

namespace Server;

public partial class DemoServer : ServerBase<DemoClient>
{
    public DemoServer(int maxSessionCount = 10) : base(maxSessionCount)
    {
        
    }
    
    protected override void OnInit()
    {
        //InitDebug();
    }

    protected override void OnUpdate()
    {
        
    }

    protected override void OnDeinit()
    {
        
    }
    
    private int _handleCount = 0;
    
    public void InitDebug()
    {
        var debugTimer = new Timer(1000);
        debugTimer.Elapsed += HandleDebug;
        debugTimer.Start();
    }

    private void HandleDebug(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        Logger.Debug($"Messages Handled: {_handleCount}");
        _handleCount = 0;
        
        Router.Debug();
        NetworkListener.Debug();
    }
}