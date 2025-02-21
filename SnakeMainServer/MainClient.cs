using Server;
using Shared;
using Shared.Logger;
using Shared.Network;
using SnakeMainServer.PO;

namespace SnakeMainServer;

public class MainClient : ClientBase<MainClient>
{
    public uint PlayerId { get; private set; }
    public string Username { get; private set; }
    public uint Coins { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void SetDataAfterLoginSuccess(PlayerPO playerPo)
    {
        if (PlayerId != 0)
        {
            Log.Error($"Player {PlayerId} 重複設置登入資料");
            return;
        }
        
        PlayerId = playerPo.PlayerId;
        Username = playerPo.Username;
        Coins = playerPo.Coins;
        CreatedAt = playerPo.CreatedAt;
    }
    
    public void SendStateCode(StateCode stateCode)
    {
        var response = ByteBufferPool.Shared.Rent(4);
        response.WriteUInt32((uint)stateCode);
        SendMessage((ushort)MessageId.M2C_StateCode, response);
        ByteBufferPool.Shared.Return(response);
    }
}