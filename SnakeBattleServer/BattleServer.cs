using Server;

namespace SnakeBattleServer;

public partial class BattleServer : ServerBase<BattleClient>
{
    private Dictionary<string, Room> _keyToRooms = new Dictionary<string, Room>();
    
    public BattleServer(ServerSettings settings) : base(settings)
    {
    }

    protected override void OnFixedUpdate()
    {
        foreach (var pairs in _keyToRooms)
        {
            pairs.Value.FixedUpdate();
        }
    }
}