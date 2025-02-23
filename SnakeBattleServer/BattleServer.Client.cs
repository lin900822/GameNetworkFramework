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
            return;

        if (!_keyToRooms.TryGetValue(c2BJoinRoom.KeyToEnterRoom, out var room))
        {
            Log.Warn($"找不到Room {c2BJoinRoom.KeyToEnterRoom} PlayerId {c2BJoinRoom.PlayerId}");
            return;
        }
        
        Log.Info($"Player {c2BJoinRoom.PlayerId} 加入 Room {c2BJoinRoom.KeyToEnterRoom}");
    }
}