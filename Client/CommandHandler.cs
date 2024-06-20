using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Core.Common;
using Core.Logger;
using Core.Network;
using Shared;

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

public partial class CommandHandler
{
    #region - Inner Logic -

    private NetworkAgent _networkAgent;

    private ConcurrentQueue<Action>    _inputActions;
    private Dictionary<string, Action> _commandHandlers;

    public CommandHandler(NetworkAgent networkAgent, ConcurrentQueue<Action> inputActions)
    {
        _networkAgent = networkAgent;
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

            var parameters       = methodInfo.GetParameters();
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

    [Command("echo")]
    public async void TestEcho()
    {
        await SendEcho(false);
    }

    [Command("echoasync")]
    public async void TestEchoAsync()
    {
        await SendEcho(true);
    }

    private async Task SendEcho(bool isAsync)
    {
        YieldToMainThread(async () =>
        {
            int    length      = 1024 * 100;
            byte[] randomBytes = GenerateRandomBytes(length);

            // 将字节数组转换为 Base64 字符串
            string randomString = Convert.ToBase64String(randomBytes);

            var echo     = new Echo() { Content = randomString };
            var echoData = ProtoUtils.Encode(echo);
            Log.Info($"Data Length: {echoData.Length}");

            Log.Info($"Before await Thread:{Environment.CurrentManagedThreadId}");
            ReceivedMessageInfo messageInfo;
            if (isAsync)
            {
                messageInfo = await _networkAgent.SendRequest((ushort)MessageId.EchoAsync, echoData);
            }
            else
            {
                messageInfo = await _networkAgent.SendRequest((ushort)MessageId.Echo, echoData);
            }

            Log.Info($"After  await Thread:{Environment.CurrentManagedThreadId}");

            if (!messageInfo.TryDecode<Echo>(out var response)) return;
            if (response.Content == echo.Content)
                Log.Info("True");
            else
                Log.Info("False");
        });
    }

    [Command("hello")]
    public async void TestHello()
    {
        await SendHello();
    }

    private async Task SendHello()
    {
        YieldToMainThread(() =>
        {
            var hello     = new Hello() { Content = "Hello from Client" };
            var helloData = ProtoUtils.Encode(hello);
            _networkAgent.SendMessage((ushort)MessageId.Hello, helloData);
        });
    }

    private byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        Random random      = new Random();

        random.NextBytes(randomBytes);

        return randomBytes;
    }

    [Command("ping")]
    public async void TestPing()
    {
        TestData.PingTime = TimeUtils.GetTimeStamp();
        _networkAgent.SendMessage((ushort)MessageId.HeartBeat, Array.Empty<byte>());
    }

    [Command("move")]
    public async void TestMove()
    {
        YieldToMainThread(async () =>
        {
            var move     = new Move() { X = 99 };
            var moveData = ProtoUtils.Encode(move);

            _networkAgent.SendMessage((ushort)MessageId.Move, moveData);
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
            var user     = new User() { Username = username, Password = password };
            var userData = ProtoUtils.Encode(user);

            var messageInfo = await _networkAgent.SendRequest((ushort)MessageId.Register, userData,
                () => { Log.Warn($"Time Out"); });

            var response = messageInfo.Message.ReadUInt16();

            if (response == 1)
            {
                Log.Info($"註冊成功!");
            }
            else
            {
                Log.Info($"註冊失敗: {response}");
            }
        });
    }
    
    [Command("multiregister")]
    public async void TestMultiRegister()
    {
        Log.Info("請輸入用戶名稱:");
        var username = Console.ReadLine();
        Log.Info("請輸入密碼:");
        var password = Console.ReadLine();

        YieldToMainThread(async () =>
        {
            var user     = new User() { Username = username, Password = password };
            var userData = ProtoUtils.Encode(user);

            for (int i = 0; i < 100; i++)
            {
                _networkAgent.SendRequest((ushort)MessageId.Register, userData,
                    () => { Log.Warn($"Time Out"); }).Await((response) =>
                {
                    var result = response.Message.ReadUInt16();

                    if (result == 1)
                    {
                        Log.Info($"註冊成功!");
                    }
                    else
                    {
                        Log.Info($"註冊失敗: {result}");
                    }
                });
            }
        });
    }


    [Command("login")]
    public async void TestLogin()
    {
        Log.Info("請輸入用戶名稱:");
        var username = Console.ReadLine();
        Log.Info("請輸入密碼:");
        var password = Console.ReadLine();

        YieldToMainThread(async () =>
        {
            var user     = new User() { Username = username, Password = password };
            var userData = ProtoUtils.Encode(user);

            var messageInfo = await _networkAgent.SendRequest((ushort)MessageId.Login, userData,
                () => { Log.Warn($"Time Out"); });

            var response = messageInfo.Message.ReadUInt16();

            if (response == 1)
            {
                Log.Info($"登入成功!");
            }
            else
            {
                Log.Info($"{response}");
            }
        });
    }

    [Command("rawbyte")]
    public async void TestRawByte()
    {
        var request = ByteBufferPool.Shared.Rent(12);
        request.WriteUInt32(24);
        request.WriteUInt32(65);
        request.WriteUInt32(98);

        _networkAgent.SendMessage((ushort)MessageId.RawByte, request);

        ByteBufferPool.Shared.Return(request);
    }
    
    [Command("broadcast")]
    public async void TestBroadcast()
    {
        var request = ByteBufferPool.Shared.Rent(12);
        request.WriteUInt32(24);
        request.WriteUInt32(65);
        request.WriteUInt32(98);

        _networkAgent.SendMessage((ushort)MessageId.Broadcast, request);

        ByteBufferPool.Shared.Return(request);
    }
}