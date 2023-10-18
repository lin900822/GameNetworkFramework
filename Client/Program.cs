using Common;
using Log;
using Network.TCP;

MessageRouter messageRouter = new MessageRouter();
TCPConnector  connector     = new TCPConnector();

messageRouter.RegisterMessageHandler(1, (_, message) =>
{
    var hello = ProtoUtils.Decode<Hello>(message);
    Logger.Info(hello.Content);
});

connector.OnReceivedMessage += messageRouter.ReceiveMessage;
connector.Connect("127.0.0.1", 10001);

Thread.Sleep(1000);

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
        for (int i = 0; i < 100; i++)
        {
            connector.Send(1, data);
        }
        
        Thread.Sleep(10);
    }
}