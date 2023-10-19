using Common;
using Log;
using Network.TCP;

int connectionCount = 5000;

MessageRouter  messageRouter = new MessageRouter();
TCPConnector[] connectors    = new TCPConnector[connectionCount];

messageRouter.RegisterMessageHandler(1, (_, message) =>
{
    var hello = ProtoUtils.Decode<Hello>(message);
    Logger.Info(hello.Content);
});

for (int i = 0; i < connectionCount; i++)
{
    connectors[i] = new TCPConnector();

    connectors[i].OnReceivedMessage += messageRouter.ReceiveMessage;
    connectors[i].Connect("127.0.0.1", 10001);
}

Thread.Sleep(3000);

Thread sendThread = new Thread(SendLoop);
sendThread.Start();

while (true)
{
    messageRouter.OnUpdateLogic();
}

void SendLoop()
{
    var hello = new Hello() { Content = "client message" };
    var data  = ProtoUtils.Encode(hello);

    while (true)
    {
        for (int i = 0; i < connectionCount; i++)
        {
            if(!connectors[i].IsConnected) continue;

            for (int j = 0; j < 1; j++)
            {
                connectors[i].Send(1, data);
            }
        }

        Thread.Sleep(300);
    }
}