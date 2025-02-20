using Server;
using Shared.Common;
using SnakeMainServer.Repositories;

namespace SnakeMainServer;

public partial class MainServer : ServerBase<MainClient>
{
    private PlayerRepository _playerRepository;
    
    public MainServer(ServerSettings settings, PlayerRepository playerRepository) : base(settings)
    {
        _playerRepository = playerRepository;
    }

    protected override void OnInit()
    {
        _playerRepository.Init().SafeWait();
    }

    protected override void OnFixedUpdate()
    {
        
    }
    
    protected override void OnDeinit()
    {
        
    }
}