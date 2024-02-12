using System.Collections.Concurrent;
using System.Reflection;
using Core.Common;
using Core.Log;
using Core.Network;
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

    private NetworkClient _networkClient;

    private ConcurrentQueue<Action> _inputActions;
    private Dictionary<string, Action> _commandHandlers;

    public CommandHandler(NetworkClient networkClient, ConcurrentQueue<Action> inputActions)
    {
        _networkClient = networkClient;
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
                Log.Error($"重複CommandHandler!");
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

    [Command("safewait")]
    public void TestSafeWait()
    {
        YieldToMainThread(() =>
        {
            Log.Debug($"{Environment.CurrentManagedThreadId} Before SafeWait");
            DoSomethingHeavy().SafeWait();
            Log.Debug($"{Environment.CurrentManagedThreadId} After  SafeWait");
        });
    }

    [Command("safewaitotherthread")]
    public void TestSafeWaitOtherThread()
    {
        Log.Debug($"{Environment.CurrentManagedThreadId} Before SafeWait");
        DoSomethingHeavy().SafeWait();
        Log.Debug($"{Environment.CurrentManagedThreadId} After  SafeWait");
    }

    private async Task DoSomethingHeavy()
    {
        Log.Debug($"{Environment.CurrentManagedThreadId} Before Heavy Work");
        await Task.Delay(1500);
        Log.Debug($"{Environment.CurrentManagedThreadId} After Heavy Work");
    }

    [Command("fireandforget")]
    public void TestFireAndForget()
    {
        Log.Debug("Before Task");
        DoSomething().Await(() => { Log.Info("Completed!"); }, (e) => { Log.Error($"{e.Message}"); });
        Log.Debug("After Task");
    }

    private async Task DoSomething()
    {
        await Task.Delay(0);
        SomethingWrong();
    }

    private void SomethingWrong() => throw new Exception("Error!");

    [Command("await")]
    public async void TestAwait()
    {
        Log.Info($"Before await Thread:{Environment.CurrentManagedThreadId}");
        await Task.Delay(1);
        Log.Info($"After  await Thread:{Environment.CurrentManagedThreadId}");

        YieldToMainThread(async () => { });
    }

    [Command("hello")]
    public async void TestHello()
    {
        await SendHello();
    }

    private async Task SendHello()
    {
        YieldToMainThread(async () =>
        {
            int length = 1024 * 100;
            byte[] randomBytes = GenerateRandomBytes(length);

            // 将字节数组转换为 Base64 字符串
            string randomString = Convert.ToBase64String(randomBytes);
            
            var hello = new Hello() { Content = randomString };
            var helloData = ProtoUtils.Encode(hello);
            Log.Info($"Data Length: {helloData.Length}");

            Log.Info($"Before await Thread:{Environment.CurrentManagedThreadId}");
            var messageInfo = await _networkClient.SendRequest((ushort)MessageId.Hello, helloData);
            Log.Info($"After  await Thread:{Environment.CurrentManagedThreadId}");

            if (!messageInfo.TryDecode<Hello>(out var response)) return;
            if(response.Content == hello.Content)
                Log.Info("True");
            else
                Log.Info("False");
            //Logger.Info($"StateCode({messageInfo.StateCode}): " + response.Content);
        });
    }
    
    private byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        Random random = new Random();

        random.NextBytes(randomBytes);

        return randomBytes;
    }

    [Command("move")]
    public async void TestMove()
    {
        YieldToMainThread(async () =>
        {
            var move = new Move() { X = 99 };
            var moveData = ProtoUtils.Encode(move);

            _networkClient.SendMessage((ushort)MessageId.Move, moveData);
        });
    }

    [Command("register")]
    public async void TestRegister()
    {
        Log.Info("請輸入用戶名稱:");
        var username = Console.ReadLine();
        Log.Info("請輸入密碼:");
        var password = Console.ReadLine();

        YieldToMainThread(async () =>
        {
            var user = new User() { Username = username, Password = password };
            var userData = ProtoUtils.Encode(user);

            var messageInfo = await _networkClient.SendRequest((ushort)MessageId.Register, userData,
                () => { Log.Warn($"Time Out"); });

            // if (messageInfo.StateCode == (uint)StateCode.Success)
            // {
            //     Log.Info($"註冊成功!");
            // }
            // else
            // {
            //     Log.Info($"{messageInfo.StateCode.ToString()}");
            // }
        });
    }
}