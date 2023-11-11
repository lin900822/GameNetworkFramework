using Log;
using Network;

int connectionCount = 10;

MessageRouter messageRouter = new MessageRouter();
NetworkConnector[] connectors = new NetworkConnector[connectionCount];

messageRouter.RegisterMessageHandler(1, (messagePack) =>
{
    if (!messagePack.TryDecode<Hello>(out var hello)) return;
    Logger.Info(hello.Content);
});

for (int i = 0; i < connectionCount; i++)
{
    connectors[i] = new NetworkConnector();

    connectors[i].OnReceivedMessage += messageRouter.ReceiveMessage;
    connectors[i].Connect("127.0.0.1", 10001);
}

Thread.Sleep(1000);

Thread sendThread = new Thread(SendLoop);
sendThread.Start();

while (true)
{
    messageRouter.OnUpdateLogic();
}

void SendLoop()
{
    var hello = new Hello() { Content = "client message 66666666666666666666" };
    var data = ProtoUtils.Encode(hello);

    var move  = new Move() { X = 1, Y = 2, Z = 3};
    var data2 = ProtoUtils.Encode(move);

    while (true)
    {
        for (int i = 0; i < connectionCount; i++)
        {
            if (!connectors[i].IsConnected) continue;

            for (int j = 0; j < 1; j++)
            {
                connectors[i].Send(1, data);
                connectors[i].Send(2, data2);
            }
        }
        Thread.Sleep(150);
    }
}