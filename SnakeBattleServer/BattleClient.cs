using Server;

namespace SnakeBattleServer;

public class BattleClient : ClientBase<BattleClient>
{
    public uint ServerId { get; set; }
    public uint PlayerId { get; set; }
    public string KeyToEnterRoom { get; set; }
    
    public SnakeUnit SnakeUnit { get; set; }

    public bool IsMainServer()
    {
        return ServerId > 0;
    }

    public bool IsPlayer()
    {
        return PlayerId > 0;
    }
}