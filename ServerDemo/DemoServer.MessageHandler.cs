using System.Text;
using Core.Common;
using Core.Logger;
using Core.Network;
using Server;
using ServerDemo.PO;
using Shared;

namespace ServerDemo;

public partial class DemoServer
{
    [MessageRoute((ushort)MessageId.Hello)]
    public async Task<bool> OnReceiveHello(DemoClient client, ByteBuffer request, ByteBuffer response)
    {
        if (!request.TryDecode<Hello>(out var hello))
        {
            return false;
        }

        await Task.Delay(1000);

        var helloData = ProtoUtils.Encode(hello);

        response.Write(helloData, 0, helloData.Length);
        return true;
    }

    [MessageRoute((ushort)MessageId.Move)]
    public void OnReceiveMove(DemoClient client, ByteBuffer request)
    {
        if (!request.TryDecode<Move>(out var move)) return;
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum++;
        }

        move.X += sum;
        var moveData   = ProtoUtils.Encode(move);
        
        var message = ByteBufferPool.Shared.Rent(moveData.Length);
        message.Write(moveData, 0, moveData.Length);
        client.SendMessage((ushort)MessageId.Move, message);
        ByteBufferPool.Shared.Return(message);
    }

    private static AwaitLock _awaitLock = new AwaitLock();
    
    [MessageRoute((ushort)MessageId.Register)]
    public async Task<bool> OnReceiveUserRegister(DemoClient client, ByteBuffer request, ByteBuffer response)
    {
        Log.Debug("Before AwaitLock");

        if (!request.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return false;
        }

        using (await _awaitLock.Lock())
        {
            Log.Debug("After AwaitLock");

            if (user.Username.Length <= 2)
            {
                response.WriteUInt16(0);
                return false;
            }

            var isUserExist = await _userRepository.IsUserExist(user.Username);
            if (isUserExist)
            {
                response.WriteUInt16(0);
                Log.Debug($"{Environment.CurrentManagedThreadId}: {user.Username} 已存在");
                return false;
            }

            // 這裡應該要改成maxId是在Server啟動時Cache到Memory
            var maxId = await _userRepository.GetMaxId();

            await _userRepository.Insert(new UserPO()
            {
                Id       = maxId + 1,
                Username = user.Username,
                Password = user.Password,
            });

            Log.Debug($"{Environment.CurrentManagedThreadId}: {user.Username} 註冊成功");

            response.WriteUInt16(1);
            return true;
        }
    }

    [MessageRoute((ushort)MessageId.Login)]
    public async Task<bool> OnReceiveUserLogin(DemoClient client, ByteBuffer request, ByteBuffer response)
    {
        if (!request.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return false;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            return false;
        }

        var userPo = await _userRepository.GetUserAsync(user.Username);
        if (userPo != null)
        {
            if (userPo.Password.Equals(user.Password))
            {
                Log.Info($"玩家[{user.Username}] 登入成功");
                response.WriteUInt16(1);
            }
            else
            {
                Log.Info($"玩家[{user.Username}] 密碼錯誤");
                response.WriteUInt16(0);
            }
        }
        else
        {
            Log.Info($"玩家[{user.Username}] 不存在");
            response.WriteUInt16(0);
        }

        return true;
    }

    [MessageRoute((ushort)MessageId.RawByte)]
    public void OnReceiveRawByte(DemoClient client, ByteBuffer request)
    {
        var x = request.ReadUInt32();
        var y = request.ReadUInt32();
        var z = request.ReadUInt32();

        client.SendMessage((ushort)MessageId.RawByte, _cacheRawByteData);
    }

    [MessageRoute((ushort)MessageId.Broadcast)]
    public void OnReceiveBroadcast(DemoClient client, ByteBuffer request)
    {
        var x = request.ReadUInt32();
        var y = request.ReadUInt32();
        var z = request.ReadUInt32();

        BroadcastMessage((ushort)MessageId.Broadcast, _cacheRawByteData);
    }
}