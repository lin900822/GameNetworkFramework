using Core.Log;
using Core.Network;
using Protocol;
using Server;
using ServerDemo.PO;

namespace ServerDemo;

public partial class DemoServer
{
    private int _lastCount;
    
    [MessageRoute((uint)MessageId.Hello)]
    public async Task<Response> OnReceiveHello(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello))
        {
            return Response.None;
        }

        var helloData = ProtoUtils.Encode(hello);

        // var response = await _connectorClient.SendRequest((uint)MessageId.Hello, helloData);
        //
        // if (!response.TryDecode<Hello>(out hello))
        // {
        //     return Response.None;
        // }
        //
        // helloData = ProtoUtils.Encode(hello);

        _lastCount++;
        
        return Response.Create(helloData);
    }
    
    [MessageRoute((uint)MessageId.Move)]
    public void OnReceiveMove(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Move>(out var move)) return;
        _lastCount++;
        //Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
    
    [MessageRoute((uint)MessageId.Register)]
    public async Task<Response> OnReceiveUserRegister(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return Response.None;
        
        if(user.Username.Length <= 2) return Response.None;

        Logger.Debug($"{Environment.CurrentManagedThreadId}: Before IsUserExist");
        var isUserExist = await _userRepository.IsUserExist(user.Username);
        Logger.Debug($"{Environment.CurrentManagedThreadId}: After IsUserExist");
        if (isUserExist)
        {
            return Response.Create((uint)StateCode.Register_Failed_UserExist);
        }
        
        Logger.Debug($"{Environment.CurrentManagedThreadId}: Before Insert");
        
        await _userRepository.Insert(new UserPO()
        {
            Username = user.Username,
            Password = user.Password,
        });
        
        Logger.Debug($"{Environment.CurrentManagedThreadId}: After Insert");

        return Response.Create((uint)StateCode.Success);
    }
}