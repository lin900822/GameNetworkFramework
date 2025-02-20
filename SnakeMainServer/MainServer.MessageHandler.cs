using Server;
using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeMainServer;

public partial class MainServer
{
    [MessageRoute((ushort)MessageId.C2M_PlayerLoginOrRegister)]
    public void C2M_PlayerLoginOrRegister(MainClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out C2M_PlayerLoginOrRegister playerData))
            return;

        if (string.IsNullOrWhiteSpace(playerData.Username) || string.IsNullOrWhiteSpace(playerData.Password))
        {
            client.SendStateCode(StateCode.LoginOrRegister_Failed_InfoEmpty);
            return;
        }
        
        Log.Info($"Player Info: {playerData.Username} {playerData.Password}");

        if (playerData.IsLogin)
        {
            client.SendStateCode(StateCode.Login_Success);
        }
        else
        {
            client.SendStateCode(StateCode.Register_Success);
        }
    }
}