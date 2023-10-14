using System.Net.Sockets;

namespace Network.TCP;

public class TCPClient
{
    public Socket            Socket;
    public ByteBuffer        ReadBuffer = new ByteBuffer();
    public Queue<ByteBuffer> SendQueue  = new Queue<ByteBuffer>();
}