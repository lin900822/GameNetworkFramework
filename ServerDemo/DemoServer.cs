using System.Diagnostics;
using Core.Common;
using Core.Log;
using Core.Metrics;
using Server;
using ServerDemo.Repositories;
using Debugger = Core.Metrics.Debugger;

namespace ServerDemo;

public partial class DemoServer : ServerBase<DemoClient>
{
    private UserRepository _userRepository;
    //private ClientBase _connectorClient;

    private Debugger _debugger;
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;

        //_connectorClient = new ClientBase();
    }
    
    protected override void OnInit()
    {
        //InitDebug();
        //_connectorClient.Connect("127.0.0.1", 10002);

        _debugger = new Debugger();
        _debugger.Start(1000, () =>
        {
            Console.WriteLine($"Session Count: {SessionList.Count} Handled Queue Count: {SystemMetrics.HandledMessageCount} Remain: {SystemMetrics.RemainMessageCount}");
            SystemMetrics.HandledMessageCount = 0;
        });
    }

    protected override void OnUpdate()
    {
        //_connectorClient.Update();
    }

    protected override void OnDeinit()
    {
        
    }
}