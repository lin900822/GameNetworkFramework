using Server;
using Shared;
using Shared.Logger;
using Shared.Network;
using Shared.Server;

namespace SnakeBattleServer;

public partial class BattleServer
{
    [MessageRoute((ushort)BattleMessageId.M2B_HandShake)]
    public async Task M2B_HandShake(BattleClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out M2B_HandShake m2BHandShake))
            return;

        if (m2BHandShake.Password.Equals("SnakeMainServerPassword2025"))
        {
            Log.Info($"MainServer {m2BHandShake.ServerId} 成功連接至 BattleServer");
            client.ServerId = m2BHandShake.ServerId;
        }
        else
        {
            Log.Error($"BattleServer收到不明連線");
            client.Communicator.CloseCommunicator();
        }
    }
}