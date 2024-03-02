using System.Collections.Concurrent;

namespace Core.Network;

public class NetworkCommunicatorPool
{
    private ConcurrentStack<NetworkCommunicator> _communicatorStack;

    public NetworkCommunicatorPool(int maxConnectionCount)
    {
        var bufferSize = NetworkConfig.BufferSize;
        
        _communicatorStack = new ConcurrentStack<NetworkCommunicator>();
        
        for (int i = 0; i < maxConnectionCount; i++)
        {
            var communicator = new NetworkCommunicator(bufferSize);

            _communicatorStack.Push(communicator);
        }
    }

    public NetworkCommunicator Rent()
    {
        if (_communicatorStack.TryPop(out var communicator)) return communicator;

        return null;
    }

    public void Return(NetworkCommunicator communicator)
    {
        communicator.Release();
        _communicatorStack.Push(communicator);
    }
}