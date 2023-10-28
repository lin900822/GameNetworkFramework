using System.Collections.Concurrent;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkSessionPool
{
    private const int MAX_SESSION_COUNT = 5000; 
    private const int BUFFER_SIZE       = 1024 * 16; // 16kb

    private ConcurrentQueue<NetworkSession> _sessionQueue;
    private byte[]                          _argsBuffer;

    public NetworkSessionPool()
    {
        _argsBuffer = new byte[2 * MAX_SESSION_COUNT * BUFFER_SIZE]; // 2代表一個Session會有2個Args(Receive, Send)
        
        _sessionQueue = new ConcurrentQueue<NetworkSession>();
        
        for (int i = 0; i < MAX_SESSION_COUNT; i++)
        {
            var session    = new NetworkSession();
            var receiveArg = new SocketAsyncEventArgs();
            var sendArg    = new SocketAsyncEventArgs();
            
            receiveArg.SetBuffer(_argsBuffer, (i) * BUFFER_SIZE, BUFFER_SIZE);
            sendArg.SetBuffer(_argsBuffer, (i + MAX_SESSION_COUNT) * BUFFER_SIZE, BUFFER_SIZE);

            session.ReceiveArgs = receiveArg;
            session.SendArgs    = sendArg;
            
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