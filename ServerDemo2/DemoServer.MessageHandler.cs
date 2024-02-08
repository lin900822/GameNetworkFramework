using Core.Network;
using Protocol;
using Server;

namespace ServerDemo2;

public partial class DemoServer
{
    [MessageRoute(MessageId.Hello)]
    public Response OnReceiveHello(ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello)) return Response.None;
        
        hello.Content = $"Server 2 Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);

        
        return Response.Create(data);
    }
}