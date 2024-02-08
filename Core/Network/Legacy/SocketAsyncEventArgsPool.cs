using System.Net.Sockets;
using Core.Log;

namespace Core.Network.Legacy;

public class SocketAsyncEventArgsPool
{
    public int Count => _eventArgsStack.Count;
    
    private Stack<SocketAsyncEventArgs> _eventArgsStack; 
    
    private BufferManager _bufferManager;

    public SocketAsyncEventArgsPool(int capacity, int bufferSize)
    {
        var initSize = capacity * bufferSize;
        _bufferManager  = new BufferManager(initSize, bufferSize);
        _eventArgsStack = new Stack<SocketAsyncEventArgs>();
        
        for (int i = 0; i < capacity; i++)
        {
            var e = new SocketAsyncEventArgs();
            _eventArgsStack.Push(e);
        }
    }

    public void Return(SocketAsyncEventArgs e)
    {
        if (e == null) throw new ArgumentNullException();
        
        lock (this)
        {
            _bufferManager.FreeBuffer(e);
            _eventArgsStack.Push(e);
        }
    }

    public SocketAsyncEventArgs Get()
    {
        lock (this)
        {
            if (_eventArgsStack.Count == 0)
            {
                var newArg = new SocketAsyncEventArgs();
                _bufferManager.SetBuffer(newArg);
                return newArg;
            }

            var e = _eventArgsStack.Pop();
            _bufferManager.SetBuffer(e);
            
            return e;
        }
    }

    public void Debug()
    {
        Log.Log.Debug($"SocketAsyncEventArgsPool {Count}, BufferManager {_bufferManager.Size}");
    }
}