using System.Collections.Concurrent;

namespace Core.Network;

public class NetworkSessionPool
{
    private ConcurrentStack<NetworkSession> _sessionStack;

    public NetworkSessionPool(int maxSessionCount)
    {
        var bufferSize = NetworkConfig.BufferSize;
        
        _sessionStack = new ConcurrentStack<NetworkSession>();
        
        for (int i = 0; i < maxSessionCount; i++)
        {
            var session = new NetworkSession(bufferSize);

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
        session.Release();
        _sessionStack.Push(session);
    }
}