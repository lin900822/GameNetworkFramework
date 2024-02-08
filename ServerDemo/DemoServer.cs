using System.Diagnostics;
using System.Timers;
using Core.Log;
using Server;
using ServerDemo.Repositories;
using Timer = System.Timers.Timer;
using ClientBase = Client.ClientBase;

namespace ServerDemo;

public partial class DemoServer : ServerBase<DemoClient>
{
    private UserRepository _userRepository;
    //private ClientBase _connectorClient;

    private Stopwatch _stopwatch;
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;

        //_connectorClient = new ClientBase();
    }
    
    protected override void OnInit()
    {
        //InitDebug();
        //_connectorClient.Connect("127.0.0.1", 10002);
        
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    protected override void OnUpdate()
    {
        //_connectorClient.Update();

        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            _stopwatch.Restart();
        }
    }

    protected override void OnDeinit()
    {
        
    }
}