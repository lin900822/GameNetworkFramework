using System.Diagnostics;
using Client;
using Core.Logger;
using Core.Network;
using Protocol;

var move = new Move();
move.X = 10;
var moveData = ProtoUtils.Encode(move);

int length = 256;
byte[] randomBytes = GenerateRandomBytes(length);
string randomString = Convert.ToBase64String(randomBytes);

var hello = new Hello() { Content = "Username Password Email Data Query MySQL Server" };
var helloData = ProtoUtils.Encode(hello);

var responseCount = 0;

var stopWatch = new Stopwatch();
stopWatch.Start();

var threadCount = 24;
var botCount = 41;

for (int i = 0; i < threadCount; i++)
{
    Task.Run(() =>
    {
        var bots = new NetworkClient[botCount];
        for (int j = 0; j < botCount; j++)
        {
            bots[j] = new NetworkClient();
            bots[j].RegisterMessageHandler((ushort)MessageId.Move, (receivedMessageInfo) =>
            {
                // if(receivedMessageInfo.TryDecode<Move>(out move))
                // Log.Info($"{move.X}");
            });
            bots[j].RegisterMessageHandler(1, (receivedMessageInfo) =>
            {
                // if(receivedMessageInfo.TryDecode<Move>(out move))
                // Log.Info($"{move.X}");
            });
        }

        foreach (var bot in bots)
        {
            bot.Connect("192.168.0.108", 10001);
            Thread.Sleep(100);
        }

        while (true)
        {
            foreach (var bot in bots)
            {
                for (var j = 0; j < 1; j++)
                {
                     // bot.SendRequest((ushort)MessageId.Hello, helloData, (receivedMessageInfo) =>
                     // {
                     //     if (receivedMessageInfo.TryDecode<Hello>(out var hello))
                     //     {
                     //         Log.Info("Get Response");
                     //     }
                     // });
                    bot.SendMessage((ushort)MessageId.HeartBeat, Array.Empty<byte>());
                }

                bot.Update();
                
            }
            Thread.Sleep(16);
        }
    });
}

while (true)
{
    if (stopWatch.ElapsedMilliseconds >= 1000)
    {
        stopWatch.Restart();
        //Log.Info($"Response Count: {responseCount}");
        Interlocked.Exchange(ref responseCount, 0);
    }
}

byte[] GenerateRandomBytes(int length)
{
    byte[] randomBytes = new byte[length];
    Random random = new Random();

    random.NextBytes(randomBytes);

    return randomBytes;
}