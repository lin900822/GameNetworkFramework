using System.Timers;
using Log;
using Network;
using Timer = System.Timers.Timer;

namespace Server;

public class TestMessageHandler : MessageHandler
{
    private int _handleCount = 0;
    
    public TestMessageHandler(ServerApp serverApp) : base(serverApp)
    {
        var debugTimer = new Timer(1000);
        debugTimer.Elapsed += HandleDebug;
        debugTimer.Start();
    }

    private void HandleDebug(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        Logger.Debug($"Messages Handled: {_handleCount}");
        _handleCount = 0;
        
        _serverApp.Router.Debug();
        _serverApp.NetworkListener.Debug();
    }
    
    [MessageRoute(1)]
    public void OnReceiveHello(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Hello>(out var hello)) return;
        
        hello.Content = $"Server Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);
        _serverApp.Send(messagePack.Session, 1, data);

        _handleCount++;
    }
    
    [MessageRoute(2)]
    public void OnReceiveMove(MessagePack messagePack)
    {
        if (!messagePack.TryDecode<Move>(out var move)) return;
        
        Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
}