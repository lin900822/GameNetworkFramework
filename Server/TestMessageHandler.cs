using Log;
using Network;

namespace Server;

public class TestMessageHandler : MessageHandler
{
    public TestMessageHandler(ServerApp serverApp) : base(serverApp)
    {
    }
    
    [Route(1)]
    public void OnReceiveHello(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Hello>(out var hello)) return;
        
        hello.Content = $"Server Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);
        _serverApp.Send(messagePack.Session, 1, data);
    }
    
    [Route(2)]
    public void OnReceiveMove(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Move>(out var move)) return;
        
        Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
}