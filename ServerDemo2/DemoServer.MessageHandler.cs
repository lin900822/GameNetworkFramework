using Core.Network;
using Server;
using Shared;

namespace ServerDemo2;

public partial class DemoServer
{
    [MessageRoute((ushort)MessageId.Hello)]
    public ByteBuffer OnReceiveHello(DemoClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<Hello>(out var hello)) return null;
        
        hello.Content = $"Server 2 Response: {hello.Content}";
        var data = ProtoUtils.Encode(hello);

        return null;
    }
}