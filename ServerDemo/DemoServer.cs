using System.Diagnostics;
using Core.Common;
using Core.Logger;
using Core.Metrics;
using Core.Network;
using Server;
using ServerDemo.Repositories;
using Debugger = Core.Metrics.Debugger;

namespace ServerDemo;

public partial class DemoServer : ServerBase<DemoClient>
{
    private UserRepository _userRepository;
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;
    }
    
    protected override void OnInit()
    {
        _userRepository.Init().SafeWait();
    }

    protected override void OnFixedUpdate()
    {
        
    }

    protected override void OnDeinit()
    {
        
    }
}