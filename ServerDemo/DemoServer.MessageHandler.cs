using Log;
using Network;
using Protocol;
using Server;
using ServerDemo.PO;

namespace ServerDemo;

public partial class DemoServer
{
    private int _lastCount;
    
    [MessageRoute((uint)MessageId.Hello)]
    public Response OnReceiveHello(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello)) return Response.None;
        
        hello.Content = $"Server Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);

        _handleCount++;
        if (_handleCount == 100000)
        {
            _handleCount = 0;
            Logger.Debug($"{(float)(GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) - _lastCount)}");
            _lastCount = (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2));
        }
        return new Response(data);
    }
    
    [MessageRoute((uint)MessageId.Move)]
    public void OnReceiveMove(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Move>(out var move)) return;
        
        Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
    
    [MessageRoute((uint)MessageId.Register)]
    public async Task<Response> OnReceiveUserRegister(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return Response.None;
        
        if(user.Username.Length <= 2) return Response.None;

        var isUserExist = await _userRepository.IsUserExist(user.Username);
        if (isUserExist)
        {
            return new Response(Array.Empty<byte>(), (uint)StateCode.Register_Failed_UserExist);
        }
        
        await _userRepository.Insert(new UserPO()
        {
            Username = user.Username,
            Password = user.Password,
        });

        return new Response(Array.Empty<byte>(), (uint)StateCode.Success);
    }
}