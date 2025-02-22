using Server;

namespace SnakeBattleServer;

public class BattleClient : ClientBase<BattleClient>
{
    public uint ServerId { get; set; }

    public bool IsMainServer()
    {
        return ServerId > 0;
    }
}