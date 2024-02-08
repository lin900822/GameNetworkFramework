using System.Collections.Concurrent;
using Client;
using Core.Common;
using Core.Log;

// 
ConcurrentQueue<Action> _inputActions = new ConcurrentQueue<Action>();

// Init
ClientBase clientBase = new ClientBase();
CommandHandler commandHandler = new CommandHandler(clientBase, _inputActions);

var synchronizationContext = new GameSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(synchronizationContext);

// Register Handlers
clientBase.RegisterMessageHandler(1, (messageInfo) => { Log.Info("Pong!"); });

// Connect
clientBase.Connect("127.0.0.1", 10001);

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
    clientBase.Update();
    synchronizationContext.ProcessQueue();

    if (_inputActions.TryDequeue(out var action))
    {
        action?.Invoke();
    }
}



