using Server;

namespace SnakeBattleServer;

public partial class BattleServer : ServerBase<BattleClient>
{
    
    
    public BattleServer(ServerSettings settings) : base(settings)
    {
    }
}