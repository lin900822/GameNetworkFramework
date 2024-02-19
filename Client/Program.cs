using System.Collections.Concurrent;
using Client;
using Core.Common;
using Core.Logger;
using Protocol;

// 
ConcurrentQueue<Action> _inputActions = new ConcurrentQueue<Action>();

// Init
NetworkClient networkClient = new NetworkClient();
CommandHandler commandHandler = new CommandHandler(networkClient, _inputActions);

var synchronizationContext = new GameSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(synchronizationContext);

// Register Handlers
networkClient.RegisterMessageHandler(1, (messageInfo) =>
{
    Log.Info($"Pong! {TimeUtils.GetTimeStamp() - TestData.PingTime}ms");
});
networkClient.RegisterMessageHandler((ushort)MessageId.Move, (messageInfo) =>
{
    if (messageInfo.TryDecode<Move>(out var move))
        Log.Info($"{move.X}");
});
networkClient.RegisterMessageHandler((ushort)MessageId.RawByte, (receivedMessageInfo) =>
{
    var x = receivedMessageInfo.Message.ReadUInt32();
    var y = receivedMessageInfo.Message.ReadUInt32();
    var z = receivedMessageInfo.Message.ReadUInt32();
                
    Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
});

// Connect
networkClient.Connect("192.168.0.108", 10001);

Thread.Sleep(100);

var inputThread = new Thread(() =>
{
    while (true)
    {
        var cmd = Console.ReadLine();
        commandHandler.InvokeCommand(cmd);
    }
});
inputThread.Start();

// Main Loop
while (true)
{
    networkClient.Update();
    synchronizationContext.ProcessQueue();

    if (_inputActions.TryDequeue(out var action))
    {
        action?.Invoke();
    }
}