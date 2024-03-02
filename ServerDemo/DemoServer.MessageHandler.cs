using System.Text;
using Core.Common;
using Core.Logger;
using Core.Network;
using Protocol;
using Server;
using ServerDemo.PO;

namespace ServerDemo;

public partial class DemoServer
{
    [MessageRoute((ushort)MessageId.Hello)]
    public Response OnReceiveHello(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello))
        {
            return Response.None;
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

        return Response.Create(helloData);
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
        var moveData = ProtoUtils.Encode(move);
        client.Send((ushort)MessageId.Move, moveData);
    }
    
    [MessageRoute((ushort)MessageId.Register)]
    public async Task<Response> OnReceiveUserRegister(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return Response.None;
        
        if(user.Username.Length <= 2) return Response.None;

        Log.Debug($"{Environment.CurrentManagedThreadId}: Before IsUserExist");
        var isUserExist = await _userRepository.IsUserExist(user.Username);
        Log.Debug($"{Environment.CurrentManagedThreadId}: After IsUserExist");
        if (isUserExist)
        {
            return Response.Create((uint)StateCode.Register_Failed_UserExist);
        }
        
        Log.Debug($"{Environment.CurrentManagedThreadId}: Before Insert");

        var maxId = await _userRepository.GetMaxId();
        
        await _userRepository.Insert(new UserPO()
        {
            Id = maxId + 1,
            Username = user.Username,
            Password = user.Password,
        });
        
        Log.Debug($"{Environment.CurrentManagedThreadId}: After Insert");

        return Response.Create((uint)StateCode.Success);
    }
    
    [MessageRoute((ushort)MessageId.Login)]
    public async Task<Response> OnReceiveUserLogin(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return Response.None;
        
        if(user.Username.Length <= 2) return Response.None;

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

        return Response.Create((uint)StateCode.Success);
    }

    [MessageRoute((ushort)MessageId.RawByte)]
    public void OnReceiveRawByte(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var x = receivedMessageInfo.Message.ReadUInt32();
        var y = receivedMessageInfo.Message.ReadUInt32();
        var z = receivedMessageInfo.Message.ReadUInt32();
        
        client.Send((ushort)MessageId.RawByte, _cacheRawByteData);
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