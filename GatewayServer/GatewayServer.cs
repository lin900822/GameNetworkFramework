using Core.Common;
using Core.Network;
using Protocol;
using Server;

namespace GatewayServer;

public partial class GatewayServer : ServerBase<GatewayClient>
{
    public GatewayServer(ServerSettings settings) : base(settings)
    {
    }

    protected override void OnFixedUpdate()
    {
        foreach (var communicator in ClientList.Keys)
        {
            var client = ClientList[communicator];
            if (TimeUtils.GetTimeStamp() - client.ConnectedTime >= 5000 && !client.HasLoggedIn)
            {
                Close(communicator);
            }
        }
    }

    [MessageRoute((ushort)MessageId.Login)]
    public void OnReceiveLogin(GatewayClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        client.HasLoggedIn = true;

        client.Send((ushort)MessageId.Login, receivedMessageInfo.Message);
    }
}