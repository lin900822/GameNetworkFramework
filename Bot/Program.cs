﻿using System.Diagnostics;
using Shared;
using Shared.Network;

// Move
var move = new Move();
move.X = 10;
var moveData = ProtoUtils.Encode(move);

// Hello
int length = 256;
byte[] randomBytes = GenerateRandomBytes(length);
string randomString = Convert.ToBase64String(randomBytes);

var hello = new Hello() { Content = "Username Password Email Data Query MySQL Server" };
var helloData = ProtoUtils.Encode(hello);

// Raw Byte
var byteBuffer = ByteBufferPool.Shared.Rent(12);
byteBuffer.WriteUInt32(24);
byteBuffer.WriteUInt32(65);
byteBuffer.WriteUInt32(98);

byte[] rawByteData = new byte[byteBuffer.Length];
byteBuffer.Read(rawByteData, 0, byteBuffer.Length);

// Other
var responseCount = 0;

var stopWatch = new Stopwatch();
stopWatch.Start();

// Start
var threadCount = 20;
var botCount = 50;

for (int i = 0; i < threadCount; i++)
{
    Task.Run(() =>
    {
        var bots = new NetworkAgent[botCount];
        for (int j = 0; j < botCount; j++)
        {
            bots[j] = new NetworkAgent();
            bots[j].RegisterMessageHandler((ushort)MessageId.Move, (communicator, receivedMessageInfo) =>
            {
                // if(receivedMessageInfo.TryDecode<Move>(out move))
                // Log.Info($"{move.X}");
            });
            bots[j].RegisterMessageHandler(1, (communicator, receivedMessageInfo) =>
            {
                // if(receivedMessageInfo.TryDecode<Move>(out move))
                // Log.Info($"{move.X}");
            });
            bots[j].RegisterMessageHandler((ushort)MessageId.RawByte, (communicator, byteBuffer) =>
            {
                var x = byteBuffer.ReadUInt32();
                var y = byteBuffer.ReadUInt32();
                var z = byteBuffer.ReadUInt32();
                
                //Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
            });
            bots[j].RegisterMessageHandler((ushort)MessageId.Broadcast, (communicator, byteBuffer) =>
            {
                var x = byteBuffer.ReadUInt32();
                var y = byteBuffer.ReadUInt32();
                var z = byteBuffer.ReadUInt32();
                
                //Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
            });
        }

        foreach (var bot in bots)
        {
            bot.Connect("127.0.0.1", 50001);
            // bot.Connect("192.168.201.146", 50001);
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
                    bot.SendMessage((ushort)MessageId.Move, moveData);
                }

                bot.Update();
            }
            Thread.Sleep(5);
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