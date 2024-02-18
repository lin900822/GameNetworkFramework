using System.Collections.Concurrent;

namespace Core.Network;

public class ByteBufferPool
{
    private static ByteBufferPool _shared;
    public static ByteBufferPool Shared
    {
        get
        {
            if (_shared == null)
            {
                _shared = new ByteBufferPool();
            }

            return _shared;
        }
    }
    
    private Queue<ByteBuffer> _byteBufferQueue = new Queue<ByteBuffer>();

    public ByteBufferPool()
    {
    }
    
    public ByteBuffer Rent(int size)
    {
        lock (_byteBufferQueue)
        {
            if (!_byteBufferQueue.TryDequeue(out var byteBuffer)) 
                return new ByteBuffer(size);
        
            if (byteBuffer.Capacity < size)
            {
                byteBuffer.Resize(size);
            }
            byteBuffer.SetReadIndex(0);
            byteBuffer.SetWriteIndex(0);
            return byteBuffer;
        }
    }

    public void Return(ByteBuffer byteBuffer)
    {
        lock (_byteBufferQueue)
        {
            _byteBufferQueue.Enqueue(byteBuffer);
        }
    }
}