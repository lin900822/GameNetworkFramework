using Common;
using Log;
using Network.TCP;

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

    foreach (var pair in tcpListener.Clients)
    {
        tcpListener.Send(pair.Value, 1, data);
    }
}

tcpListener.OnReceivedMessage += messageRouter.ReceiveMessage;
tcpListener.Listen("0.0.0.0", 10001);

int lastGCCount = 0;
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

    var gcCount = gen0Count + gen1Count + gen2Count;

    if (gcCount != lastGCCount)
    {
        lastGCCount = gcCount;
        Logger.Debug($"GC!! Handle Count {handleCount}");
        handleCount = 0;
    }
}