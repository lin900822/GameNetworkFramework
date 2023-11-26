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
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello)) return default;
        
        hello.Content = $"Server Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);
        //SendMessage(messagePack.Session, 101, data);

        _handleCount++;
        Logger.Debug(_handleCount.ToString());
        if (_handleCount == 100000)
        {
            _handleCount = 0;
            Logger.Debug($"{(float)(GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) - _lastCount)}");
            _lastCount = (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2));
        }
        return new Response(data, 200);
    }
    
    [MessageRoute((uint)MessageId.Move)]
    public void OnReceiveMove(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Move>(out var move)) return;
        
        Logger.Info($"({move.X},{move.Y},{move.Z})");
    }
    
    [MessageRoute(103)]
    public async Task OnReceiveUserRegister(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return;

        await _userRepository.Insert(new UserPO()
        {
            Username = user.Username,
            Password = user.Password,
        });
    }
}