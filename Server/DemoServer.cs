using System.Timers;
using Log;
using Network;
using Server.Repositories;
using Timer = System.Timers.Timer;

namespace Server;

public partial class DemoServer : ServerBase<DemoClient>
{
    private UserRepository _userRepository;
    
    public DemoServer(UserRepository userRepository, int maxSessionCount = 10) : base(maxSessionCount)
    {
        _userRepository = userRepository;
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