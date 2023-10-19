using Common;
using Log;
using Network.TCP;

AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
{
    var exception = (Exception)args.ExceptionObject;
    Logger.Error("Global Exception Handler Caught: " + exception.ToString());
}

MessageRouter messageRouter = new MessageRouter();
TCPListener   tcpListener   = new TCPListener();

int handleCount = 0;
messageRouter.RegisterMessageHandler(1, HandleHello);

void HandleHello(TCPClient client, byte[] message)
{
    handleCount++;
    var hello = ProtoUtils.Decode<Hello>(message);
    
    hello.Content = $"{hello.Content} {handleCount}";
    var data = ProtoUtils.Encode(hello);
    tcpListener.Send(client, 1, data);
}

tcpListener.OnReceivedMessage += messageRouter.ReceiveMessage;
tcpListener.Listen("0.0.0.0", 10001);

var timer = new System.Timers.Timer(1000);
timer.Elapsed += (sender, eventArgs) =>
{
    Logger.Debug($"Handle Count {handleCount} Connection Count {tcpListener.ConnectionCount}");
    handleCount = 0;
    
    tcpListener.Debug();
    messageRouter.Debug();
};

timer.Start();

int lastGCCount = 0;
int lastGCCount1 = 0;
int lastGCCount2 = 0;
while (true)
{
    messageRouter.OnUpdateLogic();

    PrintGC();
}

void PrintGC()
{
    int gen0Count, gen1Count, gen2Count;
    gen0Count = GC.CollectionCount(0);
    gen1Count = GC.CollectionCount(1);
    gen2Count = GC.CollectionCount(2);

    if (gen0Count != lastGCCount)
    {
        lastGCCount = gen0Count;
        Logger.Debug("GC 0!!");
    }
    if (gen1Count != lastGCCount1)
    {
        lastGCCount1 = gen1Count;
        Logger.Debug("GC 1!!");
    }
    if (gen2Count != lastGCCount2)
    {
        lastGCCount2 = gen2Count;
        Logger.Debug("GC 2!!");
    }
}

Console.ReadKey();