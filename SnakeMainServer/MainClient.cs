using Server;
using Shared;
using Shared.Network;

namespace SnakeMainServer;

public class MainClient : ClientBase<MainClient>
{
    public void SendStateCode(StateCode stateCode)
    {
        var response = ByteBufferPool.Shared.Rent(4);
        response.WriteUInt32((uint)stateCode);
        SendMessage((ushort)MessageId.M2C_StateCode, response);
        ByteBufferPool.Shared.Return(response);
    }
}