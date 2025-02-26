using Server;
using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeBattleServer;

public partial class BattleServer
{
    [MessageRoute((ushort)MessageId.C2B_JoinRoom)]
    public void C2B_JoinRoom(BattleClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out C2B_JoinRoom c2BJoinRoom))
        {
            Log.Error($"C2B_JoinRoom 解析失敗");
            return;
        }

        if (!_keyToRooms.TryGetValue(c2BJoinRoom.KeyToEnterRoom, out var room))
        {
            Log.Error($"KeyToEnterRoom {c2BJoinRoom.KeyToEnterRoom} 不存在, PlayerId = {c2BJoinRoom.PlayerId}");
            return;
        }
        
        Log.Debug($"Player {c2BJoinRoom.PlayerId} 加入 Room {c2BJoinRoom.KeyToEnterRoom}");
        client.KeyToEnterRoom = c2BJoinRoom.KeyToEnterRoom;
        client.PlayerId = c2BJoinRoom.PlayerId;
        room.SetPlayerClient(client);
    }
    
    [MessageRoute((ushort)MessageId.C2B_Input)]
    public void C2B_Input(BattleClient client, ByteBuffer request)
    {
        if (string.IsNullOrWhiteSpace(client.KeyToEnterRoom))
        {
            Log.Error($"C2B_Input KeyToEnterRoom不得為空 PlayerId {client.PlayerId}");
            return;
        }

        if (!_keyToRooms.TryGetValue(client.KeyToEnterRoom, out var room))
        {
            Log.Warn($"找不到Room {client.KeyToEnterRoom} PlayerId {client.PlayerId}");
            return;
        }

        int input = request.ReadInt32();
        room.SetPlayerInput(client, input);
    }
}