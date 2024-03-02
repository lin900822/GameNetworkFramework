using Core.Common;
using Server;

namespace GatewayServer;

public class GatewayClient : ClientBase<GatewayClient>
{
    public long ConnectedTime { get; set; }
    
    public bool HasLoggedIn { get; set; }

    protected override void OnInit()
    {
        ConnectedTime = TimeUtils.GetTimeStamp();
    }
}