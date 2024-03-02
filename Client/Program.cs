using System.Collections.Concurrent;
using Client;
using Core.Common;
using Core.Logger;
using Core.Network;
using Protocol;

// 
ConcurrentQueue<Action> _inputActions = new ConcurrentQueue<Action>();

// Init
NetworkAgent networkAgent = new NetworkAgent();
CommandHandler commandHandler = new CommandHandler(networkAgent, _inputActions);

var synchronizationContext = new GameSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(synchronizationContext);

// Register Handlers
networkAgent.RegisterMessageHandler(1, (communicator, messageInfo) =>
{
    Log.Info($"Pong! {TimeUtils.GetTimeStamp() - TestData.PingTime}ms");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.Move, (communicator, messageInfo) =>
{
    if (messageInfo.TryDecode<Move>(out var move))
        Log.Info($"{move.X}");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.RawByte, (communicator, receivedMessageInfo) =>
{
    var x = receivedMessageInfo.Message.ReadUInt32();
    var y = receivedMessageInfo.Message.ReadUInt32();
    var z = receivedMessageInfo.Message.ReadUInt32();
                
    Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.Broadcast, (communicator, receivedMessageInfo) =>
{
    var x = receivedMessageInfo.Message.ReadUInt32();
    var y = receivedMessageInfo.Message.ReadUInt32();
    var z = receivedMessageInfo.Message.ReadUInt32();
                
    //Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
});

// Connect
networkAgent.Connect("127.0.0.1", 10001);

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
    networkAgent.Update();
    synchronizationContext.ProcessQueue();

    if (_inputActions.TryDequeue(out var action))
    {
        action?.Invoke();
    }
}