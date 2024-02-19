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
    [MessageRoute(MessageId.Hello)]
    public Response OnReceiveHello(ReceivedMessageInfo receivedMessageInfo)
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
    
    [MessageRoute(MessageId.Move)]
    public void OnReceiveMove(ReceivedMessageInfo receivedMessageInfo)
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
        receivedMessageInfo.Session.Send((ushort)MessageId.Move, moveData);
    }
    
    [MessageRoute(MessageId.Register)]
    public async Task<Response> OnReceiveUserRegister(ReceivedMessageInfo receivedMessageInfo)
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
        
        await _userRepository.Insert(new UserPO()
        {
            Username = user.Username,
            Password = user.Password,
        });
        
        Log.Debug($"{Environment.CurrentManagedThreadId}: After Insert");

        return Response.Create((uint)StateCode.Success);
    }

    [MessageRoute(MessageId.RawByte)]
    public void OnReceiveRawByte(ReceivedMessageInfo receivedMessageInfo)
    {
        var x = receivedMessageInfo.Message.ReadUInt32();
        var y = receivedMessageInfo.Message.ReadUInt32();
        var z = receivedMessageInfo.Message.ReadUInt32();
        
        //receivedMessageInfo.Session.Send((ushort)MessageId.RawByte, _cacheRawByteData);
    }
}