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

Thread.Sleep(100);

var sendThread = new Thread(SendLoop);
sendThread.Start();

while (true)
{
    clientBase.Update();
}

void SendLoop()
{
    var hello = new Hello() { Content = "client message 666" };
    var helloData = ProtoUtils.Encode(hello);

    var move     = new Move() { X = 99 };
    var moveData = ProtoUtils.Encode(move);

    while (true)
    {
        // for (int i = 0; i < 1000; i++)
        // {
        //     clientBase.SendRequest(101, data, messageInfo =>
        //     { 
        //         if (!messageInfo.TryDecode<Hello>(out var response)) return;
        //         Logger.Info(response.Content);
        //         Logger.Info(messageInfo.StateCode.ToString());
        //     });
        // }
        // Thread.Sleep(1);
        
        var info = Console.ReadKey();
        if (info.Key == ConsoleKey.A)
        {
            clientBase.SendRequest((uint)MessageId.Hello, helloData, messageInfo =>
            { 
                if (!messageInfo.TryDecode<Hello>(out var response)) return;
                Logger.Info(response.Content);
                Logger.Info(messageInfo.StateCode.ToString());
            });
        }
        if (info.Key == ConsoleKey.B)
        {
            clientBase.SendRequest((uint)MessageId.Move, moveData, null, () =>
            {
                Logger.Warn($"Time Out!");
            });
        }
        if (info.Key == ConsoleKey.C)
        {
            Logger.Info("請輸入用戶名稱:");
            var username = Console.ReadLine();
            Logger.Info("請輸入密碼:");
            var password = Console.ReadLine();
            
            var user     = new User() { Username = username, Password = password};
            var userData = ProtoUtils.Encode(user);
            
            clientBase.SendRequest((uint)MessageId.Register, userData, (messageInfo) =>
            {
                if (messageInfo.StateCode == (uint)StateCode.Success)
                {
                    Logger.Info($"註冊成功!");
                }
                else
                {
                    Logger.Info($"{messageInfo.StateCode.ToString()}");
                }
            }, () =>
            {
                Logger.Warn($"Time Out");
            });
        }
    }
}