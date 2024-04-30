using Core.Common;
using Server;

namespace LittleOnlineGameServer;

public class LOGClient : ClientBase<LOGClient>
{
    public long ConnectedTime { get; set; }
    
    public bool HasLoggedIn { get; set; }

    protected override void OnInit()
    {
        ConnectedTime = TimeUtils.GetTimeStamp();
    }
}