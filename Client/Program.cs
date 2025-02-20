using System.Collections.Concurrent;
using Client;

using Shared;
using Shared.Common;
using Shared.Logger;
using Shared.Network;

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
networkAgent.RegisterMessageHandler((ushort)MessageId.Hello, (communicator, byteBuffer) =>
{
    if (byteBuffer.TryDecode<Hello>(out var hello))
        Log.Info($"{hello.Content}");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.Move, (communicator, byteBuffer) =>
{
    if (byteBuffer.TryDecode<Move>(out var move))
        Log.Info($"({move.X}, {move.Y}, {move.Z})");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.RawByte, (communicator, byteBuffer) =>
{
    var x = byteBuffer.ReadUInt32();
    var y = byteBuffer.ReadUInt32();
    var z = byteBuffer.ReadUInt32();
                
    Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
});
networkAgent.RegisterMessageHandler((ushort)MessageId.Broadcast, (communicator, byteBuffer) =>
{
    var x = byteBuffer.ReadUInt32();
    var y = byteBuffer.ReadUInt32();
    var z = byteBuffer.ReadUInt32();
                
    Log.Info($"{x.ToString()} {y.ToString()} {z.ToString()}");
});

// Connect
networkAgent.Connect("127.0.0.1", 50001);
// networkAgent.Connect("192.168.201.146", 50001);

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