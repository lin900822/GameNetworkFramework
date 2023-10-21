using System.Net.Sockets;

namespace Network;

public class NetworkClient
{
    public Socket               Socket;
    public ByteBuffer           ReceiveBuffer = new ByteBuffer();
    public Queue<ByteBuffer>    SendQueue  = new Queue<ByteBuffer>();
    
    public SocketAsyncEventArgs ReceiveArgs;
    public SocketAsyncEventArgs SendArgs;
}