using Server;
using ServerDemo.PO;
using Shared;
using Shared.Common;
using Shared.Logger;
using Shared.Network;

namespace ServerDemo;

public partial class DemoServer
{
    [MessageRoute((ushort)MessageId.Echo)]
    public bool C2S_Echo(DemoClient client, ByteBuffer request, ByteBuffer response)
    {
        if (!request.TryDecode<Echo>(out var echo))
        {
            return false;
        }

        var echoData = ProtoUtils.Encode(echo);
        response.Write(echoData, 0, echoData.Length);
        return true;
    }
    
    [MessageRoute((ushort)MessageId.EchoAsync)]
    public async Task<bool> C2S_EchoAsync(DemoClient client, ByteBuffer request, ByteBuffer response)
    {
        if (!request.TryDecode<Echo>(out var echo))
        {
            return false;
        }

        await Task.Delay(1000);

        var echoData = ProtoUtils.Encode(echo);
        response.Write(echoData, 0, echoData.Length);
        return true;
    }
    
    [MessageRoute((ushort)MessageId.Hello)]
    public void OnReceiveHello(DemoClient client, ByteBuffer request)
    {
        if (!request.TryDecode<Hello>(out var hello))
        {
            return;
        }
        
        Log.Info($"{hello.Content}");

        var newHello  = new Hello();
        newHello.Content = "Hello from Server";
        var helloData = ProtoUtils.Encode(newHello);

        client.SendMessage((ushort)MessageId.Hello, helloData);
    }

    [MessageRoute((ushort)MessageId.Move)]
    public void OnReceiveMove(DemoClient client, ByteBuffer request)
    {
        if (!request.TryDecode<Move>(out var move)) 
            return;
        var moveData   = ProtoUtils.Encode(move);
        client.SendMessage((ushort)MessageId.Move, moveData);
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
                return true;
            }

            var isUserExist = await _userRepository.IsUserExist(user.Username);
            if (isUserExist)
            {
                response.WriteUInt16(0);
                Log.Debug($"{Environment.CurrentManagedThreadId}: {user.Username} 已存在");
                return true;
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

        var response = ByteBufferPool.Shared.Rent(12);
        response.WriteUInt32(x);
        response.WriteUInt32(y);
        response.WriteUInt32(z);
        client.SendMessage((ushort)MessageId.RawByte, response);
        ByteBufferPool.Shared.Return(response);
    }

    [MessageRoute((ushort)MessageId.Broadcast)]
    public void OnReceiveBroadcast(DemoClient client, ByteBuffer request)
    {
        var x = request.ReadUInt32();
        var y = request.ReadUInt32();
        var z = request.ReadUInt32();

        var response = ByteBufferPool.Shared.Rent(12);
        response.WriteUInt32(x);
        response.WriteUInt32(y);
        response.WriteUInt32(z);
        BroadcastMessage((ushort)MessageId.Broadcast, response);
        ByteBufferPool.Shared.Return(response);
    }
    
    [MessageRoute((ushort)MessageId.Match)]
    public void OnReceiveMatch(DemoClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out Match match))
            return;

        Log.Info($"Player {match.PlayerId} 加入配對Queue");
        match.PlayerId = -1;
        var data = ProtoUtils.Encode(match);
        client.SendMessage((ushort)MessageId.Match, data);
    }
}