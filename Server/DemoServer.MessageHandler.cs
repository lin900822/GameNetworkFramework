using Log;
using Network;

namespace Server;

public partial class DemoServer
{
    [MessageRoute(101)]
    public void OnReceiveHello(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Hello>(out var hello)) return;
        
        hello.Content = $"Server Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);
        Send(messagePack.Session, 101, data);

        _handleCount++;
    }
    
    [MessageRoute(102)]
    public void OnReceiveMove(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Move>(out var move)) return;
        
        Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
}