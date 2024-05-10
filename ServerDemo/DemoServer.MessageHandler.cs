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
    public ByteBuffer OnReceiveHello(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello))
        {
            return null;
        }

        var helloData = ProtoUtils.Encode(hello);

        //Log.Debug($"Before await Thread: {Environment.CurrentManagedThreadId}");
        //await Task.Delay(100);
        //Log.Debug($"After await Thread: {Environment.CurrentManagedThreadId}");

        // var response = await _connectorClient.SendRequest((uint)MessageId.Hello, helloData);
        //
        // if (!response.TryDecode<Hello>(out hello))
        // {
        //     return Response.None;
        // }
        //
        // helloData = ProtoUtils.Encode(hello);

        var byteBuffer = ByteBufferPool.Shared.Rent(helloData.Length);
        byteBuffer.Write(helloData, 0, helloData.Length);
        return byteBuffer;
    }

    [MessageRoute((ushort)MessageId.Move)]
    public void OnReceiveMove(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Move>(out var move)) return;
        //Log.Debug($"({move.X},{move.Y},{move.Z})");
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum++;
        }

        move.X += sum;
        var moveData   = ProtoUtils.Encode(move);
        var byteBuffer = ByteBufferPool.Shared.Rent(moveData.Length);
        byteBuffer.Write(moveData, 0, moveData.Length);
        client.SendMessage((ushort)MessageId.Move, byteBuffer);
    }

    private static AwaitLock _awaitLock = new AwaitLock();
    
    [MessageRoute((ushort)MessageId.Register)]
    public async Task<ByteBuffer> OnReceiveUserRegister(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        Log.Debug("Before AwaitLock");
        
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return response;
        }

        using (await _awaitLock.Lock())
        {
            Log.Debug("After AwaitLock");

            if (user.Username.Length <= 2)
            {
                response.WriteUInt16(0);
                return response;
            }

            var isUserExist = await _userRepository.IsUserExist(user.Username);
            if (isUserExist)
            {
                response.WriteUInt16(0);
                Log.Debug($"{Environment.CurrentManagedThreadId}: {user.Username} 已存在");
                return response;
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
            return response;
        }
    }

    [MessageRoute((ushort)MessageId.Login)]
    public async Task<ByteBuffer> OnReceiveUserLogin(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return response;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            return response;
        }

        var userPo = await _userRepository.GetUserAsync(user.Username);
        if (userPo != null)
        {
            if (userPo.Password.Equals(user.Password))
            {
                Log.Info($"玩家[{user.Username}] 登入成功");
            }
            else
            {
                Log.Info($"玩家[{user.Username}] 密碼錯誤");
            }
        }
        else
        {
            Log.Info($"玩家[{user.Username}] 不存在");
        }

        response.WriteUInt16(1);
        return response;
    }

    [MessageRoute((ushort)MessageId.RawByte)]
    public void OnReceiveRawByte(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var x = receivedMessageInfo.Message.ReadUInt32();
        var y = receivedMessageInfo.Message.ReadUInt32();
        var z = receivedMessageInfo.Message.ReadUInt32();

        client.SendMessage((ushort)MessageId.RawByte, _cacheRawByteData);
    }

    [MessageRoute((ushort)MessageId.Broadcast)]
    public void OnReceiveBroadcast(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var x = receivedMessageInfo.Message.ReadUInt32();
        var y = receivedMessageInfo.Message.ReadUInt32();
        var z = receivedMessageInfo.Message.ReadUInt32();

        BroadcastMessage((ushort)MessageId.Broadcast, _cacheRawByteData);
    }
}