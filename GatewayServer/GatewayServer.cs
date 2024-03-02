using Server;

namespace GatewayServer;

public partial class GatewayServer: ServerBase<GatewayClient>
{
    public GatewayServer(ServerSettings settings) : base(settings)
    {
    }
}