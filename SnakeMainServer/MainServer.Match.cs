using Server;
using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeMainServer;

public partial class MainServer
{
    private Dictionary<uint, MainClient> _playerIdToClientMatch = new Dictionary<uint, MainClient>();
    
    [MessageRoute((ushort)MessageId.C2M_JoinMatchQueue)]
    public async Task C2M_JoinMatchQueue(MainClient client, ByteBuffer request)
    {
        if (_playerIdToClientMatch.ContainsKey(client.PlayerId))
        {
            client.SendStateCode(StateCode.JoinMatchQueue_Failed_AlreadyIn);
            return;
        }
        
        _playerIdToClientMatch.Add(client.PlayerId, client);
    }
    
    [MessageRoute((ushort)MessageId.C2M_CancelJoinMatchQueue)]
    public async Task C2M_CancelJoinMatchQueue(MainClient client, ByteBuffer request)
    {
        if (!_playerIdToClientMatch.ContainsKey(client.PlayerId))
        {
            client.SendStateCode(StateCode.CancelJoinMatchQueue_Failed_NOtIn);
            return;
        }
        
        _playerIdToClientMatch.Remove(client.PlayerId);
    }
}