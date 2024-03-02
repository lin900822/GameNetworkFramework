using Core.Logger;
using Core.Network;
using Server;

namespace GatewayServer;

public partial class GatewayServer: ServerBase<GatewayClient>
{
    public GatewayServer(ServerSettings settings) : base(settings)
    {
    }

    [MessageRoute(101)]
    public void OnReceiveLogin(GatewayClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        byte[] byteData = new byte[receivedMessageInfo.Message.Length];
        receivedMessageInfo.Message.Read(byteData, 0, byteData.Length);
        
        client.Send(101, byteData);
    }
}