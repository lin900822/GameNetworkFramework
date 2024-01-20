using System.Collections.Concurrent;
using System.Reflection;
using Log;
using Network;
using Protocol;

namespace Client;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Command { get; private set; }

    public CommandAttribute(string command)
    {
        Command = command;
    }
}

public class CommandHandler
{
    #region - Inner Logic -

    private ClientBase _clientBase;

    private ConcurrentQueue<Action> _inputActions;
    private Dictionary<string, Action> _commandHandlers;

    public CommandHandler(ClientBase clientBase, ConcurrentQueue<Action> inputActions)
    {
        _clientBase = clientBase;
        _inputActions = inputActions;
        
        _commandHandlers = new Dictionary<string, Action>();
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        var methodInfos = GetType().GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            if (!methodInfo.IsDefined(typeof(CommandAttribute), true)) continue;

            var parameters = methodInfo.GetParameters();
            var commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();

            var action = (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo);

            if (!_commandHandlers.TryAdd(commandAttribute.Command, action))
            {
                Logger.Error($"重複CommandHandler!");
            }
        }
    }

    private void YieldToMainThread(Action action)
    {
        _inputActions.Enqueue(action);
    }

    public void InvokeCommand(string cmd)
    {
        if (_commandHandlers.TryGetValue(cmd, out var action))
        {
            action.Invoke();
        }
    }

    #endregion
    
    [Command("hello")]
    public async void TestHello()
    {
        await SendHello();
    }
    
    private async Task SendHello()
    {
        YieldToMainThread(async () =>
        {
            var hello = new Hello() { Content = "client message 666" };
            var helloData = ProtoUtils.Encode(hello);

            Logger.Info($"Before await Thread:{Environment.CurrentManagedThreadId}");
            var messageInfo = await _clientBase.SendRequest((uint)MessageId.Hello, helloData);
            Logger.Info($"After  await Thread:{Environment.CurrentManagedThreadId}");

            if (!messageInfo.TryDecode<Hello>(out var response)) return;
            Logger.Info($"StateCode({messageInfo.StateCode}): " + response.Content);
        });
    }
    
    [Command("move")]
    public async void TestMove()
    {
        YieldToMainThread(async () =>
        {
            var move = new Move() { X = 99 };
            var moveData = ProtoUtils.Encode(move);
            
            await _clientBase.SendRequest((uint)MessageId.Move, moveData, () => { Logger.Warn($"Time Out!"); });
        });
    }
    
    [Command("register")]
    public async void TestRegister()
    {
        Logger.Info("請輸入用戶名稱:");
        var username = Console.ReadLine();
        Logger.Info("請輸入密碼:");
        var password = Console.ReadLine();

        YieldToMainThread(async () =>
        {
            var user = new User() { Username = username, Password = password };
            var userData = ProtoUtils.Encode(user);

            var messageInfo = await _clientBase.SendRequest((uint)MessageId.Register, userData, () => { Logger.Warn($"Time Out"); });
            
            if (messageInfo.StateCode == (uint)StateCode.Success)
            {
                Logger.Info($"註冊成功!");
            }
            else
            {
                Logger.Info($"{messageInfo.StateCode.ToString()}");
            }
        });
    }
}