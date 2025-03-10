using Server;
using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeMainServer;

public partial class MainServer
{
    private Dictionary<uint, MainClient> _playerIdToClientMatch = new Dictionary<uint, MainClient>();
    
    [MessageRoute((ushort)MessageId.C2M_JoinMatchQueue)]
    public void C2M_JoinMatchQueue(MainClient client, ByteBuffer request)
    {
        if (_playerIdToClientMatch.ContainsKey(client.PlayerId))
        {
            client.SendStateCode(StateCode.JoinMatchQueue_Failed_AlreadyIn);
            return;
        }
        
        _playerIdToClientMatch.Add(client.PlayerId, client);
    }
    
    [MessageRoute((ushort)MessageId.C2M_CancelJoinMatchQueue)]
    public void C2M_CancelJoinMatchQueue(MainClient client, ByteBuffer request)
    {
        if (!_playerIdToClientMatch.ContainsKey(client.PlayerId))
        {
            client.SendStateCode(StateCode.CancelJoinMatchQueue_Failed_NOtIn);
            return;
        }
        
        _playerIdToClientMatch.Remove(client.PlayerId);
    }

    private async Task MatchPlayerToBattleServer()
    {
        if (_playerIdToClientMatch.Count <= 1)
            return;

        var player1 = _playerIdToClientMatch.First();
        _playerIdToClientMatch.Remove(player1.Key);
        
        var player2 = _playerIdToClientMatch.First();
        _playerIdToClientMatch.Remove(player2.Key);

        var m2BCreateRoom = new M2B_CreateRoom()
        {
            Player1Id = player1.Key,
            Player2Id = player2.Key,
        };
        var response = await _battleAgent.SendRequest((ushort)MessageId.M2B_CreateRoom, ProtoUtils.Encode(m2BCreateRoom));
        if (!response.TryDecode(out B2M_RoomCreated b2MRoomCreated))
            return;

        var m2CRoomMatched = new M2C_RoomMatched()
        {
            KeyToEnterRoom = b2MRoomCreated.KeyToEnterRoom,
            Ip = "127.0.0.1",
            Port = 50011,
            Player1Name = player1.Value.Username,
            Player2Name = player2.Value.Username,
        };

        var data = ProtoUtils.Encode(m2CRoomMatched);

        var player1Client = player1.Value;
        player1Client.SendMessage((ushort)MessageId.M2C_RoomMatched, data);
        var player2Client = player2.Value;
        player2Client.SendMessage((ushort)MessageId.M2C_RoomMatched, data);
    }
}