using Core.Common;
using Core.Logger;

namespace Client;

public partial class CommandHandler
{
    private AwaitLock _awaitLock = new AwaitLock();

    [Command("awaitlock")]
    public async void TestAwaitLock()
    {
        YieldToMainThread(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                InnerTestAwaitLock().Await();
            }
        });
    }

    private async Task InnerTestAwaitLock()
    {
        using var awaitLock = await _awaitLock.Lock();
        Log.Debug($"start at {Environment.CurrentManagedThreadId}");
        Log.Info("1");
        await Task.Yield();
        Log.Info("2");
        await Task.Yield();
        Log.Info("3");
        Log.Debug($"end at {Environment.CurrentManagedThreadId}");
    }

    [Command("awaitnolock")]
    public async void TestAwaitNoLock()
    {
        YieldToMainThread(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                InnerTestAwaitNoLock().Await();
            }
        });
    }

    private async Task InnerTestAwaitNoLock()
    {
        Log.Debug($"start at {Environment.CurrentManagedThreadId}");
        Log.Info("1");
        await Task.Yield();
        Log.Info("2");
        await Task.Yield();
        Log.Info("3");
        Log.Debug($"end at {Environment.CurrentManagedThreadId}");
    }

    private AwaitLock _lock1 = new AwaitLock();
    private AwaitLock _lock2 = new AwaitLock();

    [Command("awaitdeadlock")]
    public async void TestAwaitDeadLock()
    {
        YieldToMainThread(async () =>
        {
            InnerTestAwaitDeadLock1().Await();
            InnerTestAwaitDeadLock2().Await();
        });
    }

    private async Task InnerTestAwaitDeadLock1()
    {
        Log.Info($"Enter InnerTestAwaitDeadLock1 at {Environment.CurrentManagedThreadId}");

        using (await _lock1.Lock())
        {
            await Task.Yield();
            using (await _lock2.Lock())
            {
                Log.Warn($"Exit InnerTestAwaitDeadLock1 at {Environment.CurrentManagedThreadId}");
            }
        }
    }

    private async Task InnerTestAwaitDeadLock2()
    {
        Log.Info($"Enter InnerTestAwaitDeadLock2 at {Environment.CurrentManagedThreadId}");

        using (await _lock2.Lock())
        {
            await Task.Yield();
            using (await _lock1.Lock())
            {
                Log.Warn($"Exit InnerTestAwaitDeadLock2 at {Environment.CurrentManagedThreadId}");
            }
        }
    }
}