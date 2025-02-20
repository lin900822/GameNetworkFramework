using Server;
using ServerDemo.Repositories;
using Shared.Common;

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