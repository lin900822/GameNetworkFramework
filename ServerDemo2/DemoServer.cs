using System.Timers;
using Server;
using Timer = System.Timers.Timer;

namespace ServerDemo2;

public partial class DemoServer : ServerBase<DemoClient>
{
    public DemoServer(ServerSettings settings) : base(settings)
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
}