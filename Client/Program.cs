using Client;
using Log;
using Network;
using Protocol;

int connectionCount = 1;

ClientBase clientBase = new ClientBase();

clientBase.RegisterMessageHandler(1, (messageInfo) =>
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

    var move     = new Move() { X = 99 };
    var moveData = ProtoUtils.Encode(move);

    while (true)
    {
        var info = Console.ReadKey();
        if (info.Key == ConsoleKey.A)
        {
            clientBase.SendRequest(101, data, messageInfo =>
            { 
                if (!messageInfo.TryDecode<Hello>(out var response)) return;
                Logger.Info(response.Content);
                Logger.Info(messageInfo.StateCode.ToString());
            });
        }
        if (info.Key == ConsoleKey.B)
        {
            clientBase.SendRequest(102, moveData, null, () =>
            {
                Logger.Warn($"Time out!");
            });
        }
    }
}