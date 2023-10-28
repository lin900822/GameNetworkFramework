using System.Timers;
using Common;
using Log;
using Network;
using Timer = System.Timers.Timer;

int lastCount = 0;
int handleCount = 0;

HandleException();

MessageRouter messageRouter = new MessageRouter();
NetworkListener networkListener = new NetworkListener(5000);

messageRouter.RegisterMessageHandler(1, HandleHello);

networkListener.OnReceivedMessage += messageRouter.ReceiveMessage;
networkListener.Listen("0.0.0.0", 10001);

var debugTimer = new Timer(1000);
debugTimer.Elapsed += HandleDebug;
debugTimer.Start();

while (true)
{
    messageRouter.OnUpdateLogic();
}

Console.ReadKey();

void HandleHello(NetworkSession client, byte[] message)
{
    handleCount++;
    var hello = ProtoUtils.Decode<Hello>(message);

    hello.Content = $"{hello.Content} {handleCount}";
    var data = ProtoUtils.Encode(hello);
    networkListener.Send(client, 1, data);

    if (handleCount % 100000 == 0)
    {
        Logger.Debug($"{(float)(GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) - lastCount)}");
        lastCount = (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2));
    }
}

void HandleDebug(object sender, ElapsedEventArgs elapsedEventArgs)
{
    Logger.Warn("--------------Debug--------------");
    Logger.Debug($"Handle Count {handleCount} Connection Count {networkListener.ConnectionCount}");
    handleCount = 0;

    networkListener.Debug();
    messageRouter.Debug();
}

void HandleException()
{
    AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

    void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = (Exception)args.ExceptionObject;
        Logger.Error("Global Exception Handler Caught: " + exception.ToString());
    }
}