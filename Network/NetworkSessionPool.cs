using System.Collections.Concurrent;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkSessionPool
{
    private ConcurrentStack<NetworkSession> _sessionStack;
    
    private ByteBufferPool _byteBufferPool; 

    public NetworkSessionPool(int maxSessionCount)
    {
        _byteBufferPool = new ByteBufferPool();
        
        var bufferSize = NetworkConfig.BufferSize;
        
        _sessionStack = new ConcurrentStack<NetworkSession>();
        
        for (int i = 0; i < maxSessionCount; i++)
        {
            var session = new NetworkSession(_byteBufferPool, bufferSize);

            _sessionStack.Push(session);
        }
    }

    public NetworkSession Rent()
    {
        if (_sessionStack.TryPop(out var session)) return session;

        return null;
    }

    public void Return(NetworkSession session)
    {
        session.SetInactive();
        _sessionStack.Push(session);
    }

    public void Debug()
    {
        Logger.Debug($"Session Pool Count: {_sessionStack.Count}");
    }
}