using System.Net.Sockets;

namespace Core.Network.Legacy;

public class BufferManager
{
    public int Size => _bufferBlock.Length;
    
    private int        _currentSize;
    private byte[]     _bufferBlock;
    private Stack<int> _indexPool;

    private int _usedIndex;
    private int _bufferSize;

    public BufferManager(int initSize, int bufferSize)
    {
        _currentSize = initSize;
        _bufferBlock = new byte[_currentSize];
        _indexPool   = new Stack<int>();

        _usedIndex  = 0;
        _bufferSize = bufferSize;
    }

    public void SetBuffer(SocketAsyncEventArgs e)
    {
        if (_indexPool.Count > 0)
        {
            e.SetBuffer(_bufferBlock, _indexPool.Pop(), _bufferSize);
        }
        else
        {
            // Resize
            if ((_currentSize - _bufferSize) < _usedIndex)
            {
                _currentSize *= 2;
                Array.Resize(ref _bufferBlock, _currentSize);
            }

            e.SetBuffer(_bufferBlock, _usedIndex, _bufferSize);
            _usedIndex += _bufferSize;
        }
    }

    public void FreeBuffer(SocketAsyncEventArgs e)
    {
        if (_indexPool.Contains(e.Offset))
        {
            Logger.Log.Error("BufferManager FreeBuffer: _indexPool Contains offset");
            return;
        }
        
        _indexPool.Push(e.Offset);
        e.SetBuffer(null, 0, 0);
    }
}