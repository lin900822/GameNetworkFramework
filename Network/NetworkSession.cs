using System.Net.Sockets;

namespace Network;

public class NetworkSession
{
    public Socket               Socket;
    public ByteBuffer           ReceiveBuffer;
    public Queue<ByteBuffer>    SendQueue;
    
    public SocketAsyncEventArgs ReceiveArgs;
    public SocketAsyncEventArgs SendArgs;

    public object SessionObject;
}