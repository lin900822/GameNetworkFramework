using System.Diagnostics;
using Client;
using Core.Log;
using Core.Network;
using Protocol;
using Timer = System.Timers.Timer;

var move = new Move();
move.X = 10;
var moveData = ProtoUtils.Encode(move);

int length = 256;
byte[] randomBytes = GenerateRandomBytes(length);
string randomString = Convert.ToBase64String(randomBytes);

var hello = new Hello() { Content = randomString };
var helloData = ProtoUtils.Encode(hello);

var responseCount = 0;

var stopWatch = new Stopwatch();
stopWatch.Start();

for (int i = 0; i < 25; i++)
{
    Task.Run(() =>
    {
        var bots = new NetworkClient[40];
        for (int j = 0; j < 40; j++)
        {
            bots[j] = new NetworkClient();
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
                    bot.SendMessage((uint)MessageId.Move, moveData);
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