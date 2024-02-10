using System.Collections.Concurrent;
using Client;
using Core.Common;
using Core.Log;

// 
ConcurrentQueue<Action> _inputActions = new ConcurrentQueue<Action>();

// Init
NetworkClient networkClient = new NetworkClient();
CommandHandler commandHandler = new CommandHandler(networkClient, _inputActions);

var synchronizationContext = new GameSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(synchronizationContext);

// Register Handlers
networkClient.RegisterMessageHandler(1, (messageInfo) => { Log.Info("Pong!"); });

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



