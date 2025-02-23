using Server;
using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeBattleServer;

public partial class BattleServer
{
    private Dictionary<string, Room> _keyToRooms = new Dictionary<string, Room>();
    
    [MessageRoute((ushort)MessageId.M2B_HandShake)]
    public void M2B_HandShake(BattleClient client, ByteBuffer request)
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

    [MessageRoute((ushort)MessageId.M2B_CreateRoom)]
    public bool M2B_CreateRoom(BattleClient client, ByteBuffer request, ByteBuffer response)
    {
        if (!request.TryDecode(out M2B_CreateRoom m2BCreateRoom))
            return false;

        string keyToEnterRoom = $"BattleServerKey{m2BCreateRoom.Player1Id}{m2BCreateRoom.Player2Id}";

        if (_keyToRooms.ContainsKey(keyToEnterRoom))
        {
            Log.Warn($"{keyToEnterRoom} 已存在");
            return false;
        }
        
        var room = new Room(keyToEnterRoom, m2BCreateRoom.Player1Id, m2BCreateRoom.Player2Id);
        _keyToRooms.Add(keyToEnterRoom, room);

        var b2MRoomCreated = new B2M_RoomCreated()
        {
            KeyToEnterRoom = keyToEnterRoom,
        };

        var data = ProtoUtils.Encode(b2MRoomCreated);
        response.Write(data, 0, data.Length);
        
        return true;
    }
}