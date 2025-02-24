using Server;

namespace SnakeBattleServer;

public class BattleClient : ClientBase<BattleClient>
{
    public uint ServerId { get; set; }
    public uint PlayerId { get; set; }
    public string KeyToEnterRoom { get; set; }

    public bool IsMainServer()
    {
        return ServerId > 0;
    }

    public bool IsClient()
    {
        return PlayerId > 0;
    }
}