using Core.Common;
using Core.Log;
using Core.Network;
using Protocol;
using Server;
using ServerDemo.PO;

namespace ServerDemo;

public partial class DemoServer
{
    [MessageRoute(MessageId.Hello)]
    public async Task<Response> OnReceiveHello(ReceivedMessageInfo receivedMessageInfo)
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
        
        var sum = 0;
        for (int i = 0; i < 1000_000_000; i++)
        {
            sum++;
        }

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
}