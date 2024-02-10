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
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;

        //_connectorClient = new ClientBase();
    }
    
    protected override void OnInit()
    {
        //InitDebug();
        //_connectorClient.Connect("127.0.0.1", 10002);
    }

    protected override void OnUpdate()
    {
        //_connectorClient.Update();
    }

    protected override void OnDeinit()
    {
        
    }
}