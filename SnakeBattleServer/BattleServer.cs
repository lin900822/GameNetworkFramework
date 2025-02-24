using Server;
using Shared;
using Shared.Logger;

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
    
    protected override void OnClientDisconnected(BattleClient client)
    {
        if (client.IsPlayer())
        {
            if(!_keyToRooms.TryGetValue(client.KeyToEnterRoom, out var room))
                return;
            
            room.ClientDisconnected(client);
            room.EndBattle(BattleEndResult.OtherDisconnected, 0);
            
            var isRemoved = _keyToRooms.Remove(client.KeyToEnterRoom);
            if (isRemoved)
            {
                Log.Warn($"玩家 {client.PlayerId} 斷線 強制結束Room {client.KeyToEnterRoom}");
            }
        }
    }
}