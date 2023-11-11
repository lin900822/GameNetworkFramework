using System.Collections.Concurrent;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkSessionPool
{
    private ConcurrentQueue<NetworkSession> _sessionQueue;

    public NetworkSessionPool(int maxSessionCount)
    {
        var bufferSize = NetworkConfig.BufferSize;
        
        _sessionQueue = new ConcurrentQueue<NetworkSession>();
        
        for (int i = 0; i < maxSessionCount; i++)
        {
            var receiveArg = new SocketAsyncEventArgs();
            var sendArg    = new SocketAsyncEventArgs();
            
            receiveArg.SetBuffer(new byte[bufferSize], 0, bufferSize);
            sendArg.SetBuffer(new byte[bufferSize], 0, bufferSize);

            var session = new NetworkSession
            {
                ReceiveBuffer = new ByteBuffer(bufferSize),
                SendQueue     = new Queue<ByteBuffer>(),
                ReceiveArgs   = receiveArg,
                SendArgs      = sendArg
            };

            _sessionQueue.Enqueue(session);
        }
    }

    public NetworkSession Rent()
    {
        if (_sessionQueue.TryDequeue(out var session)) return session;

        return null;
    }

    public void Return(NetworkSession session)
    {
        session.Socket = null;
        
        session.ReceiveArgs.AcceptSocket = null;
        session.SendArgs.AcceptSocket    = null;
        
        session.ReceiveBuffer.SetReadIndex(0);
        session.ReceiveBuffer.SetWriteIndex(0);
        
        session.SendQueue.Clear();
        
        _sessionQueue.Enqueue(session);
    }

    public void Debug()
    {
        Logger.Debug($"Session Pool Count: {_sessionQueue.Count}");
    }
}