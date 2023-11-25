using Client;
using Log;
using Network;

int connectionCount = 1;

ClientBase clientBase = new ClientBase();

clientBase.RegisterMessageHandler(1, (packet) =>
{
    Logger.Info("Pong!");
});
// clientBase.RegisterMessageHandler(101, (messagePack) =>
// {
//     if (!messagePack.TryDecode<Hello>(out var hello)) return;
//     Logger.Info(hello.Content);
// });

clientBase.Connect("127.0.0.1", 10001);

var sendThread = new Thread(SendLoop);
sendThread.Start();

while (true)
{
    clientBase.Update();
}

void SendLoop()
{
    var hello = new Hello() { Content = "client message 666" };
    var data = ProtoUtils.Encode(hello);

    while (true)
    {
        var info = Console.ReadKey();
        if (info.Key == ConsoleKey.A)
        {
            clientBase.SendRequest(101, data, packet =>
            { 
                if (!packet.TryDecode<Hello>(out var response)) return;
                Logger.Info(response.Content);
                Logger.Info(packet.StateCode.ToString());
            });
        }
    }
}