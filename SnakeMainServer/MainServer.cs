using Server;
using Shared;
using Shared.Common;
using Shared.Logger;
using SnakeMainServer.Repositories;

namespace SnakeMainServer;

public partial class MainServer : ServerBase<MainClient>
{
    private Dictionary<uint, MainClient> _playerIdToMainClient;

    private uint _nextPlayerId = 0;

    private PlayerRepository _playerRepository;

    public MainServer(ServerSettings settings, PlayerRepository playerRepository) : base(settings)
    {
        _playerRepository = playerRepository;
    }

    protected override void OnInit()
    {
        _playerIdToMainClient = new Dictionary<uint, MainClient>();

        _playerRepository.Init().SafeWait();

        _nextPlayerId = _playerRepository.GetMaxPlayerId().SafeWait();
        _nextPlayerId++;
    }

    protected override void OnUpdate()
    {
        RoutineScheduler.Instance.Update();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnDeinit()
    {
    }

    protected override void OnHeartBeatTimeOut(MainClient client)
    {
        if (!_playerIdToMainClient.ContainsKey(client.PlayerId))
            return;

        client.SendStateCode(StateCode.TimeOut);
        _playerIdToMainClient.Remove(client.PlayerId);
    }

    protected override void OnClientConnected(MainClient client)
    {
        
    }

    protected override void OnClientDisconnected(MainClient client)
    {
        if (client.IsLoggedIn())
        {
            _playerIdToMainClient.Remove(client.PlayerId);
        }
    }
}