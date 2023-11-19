using System.Collections.Concurrent;

namespace Network;

public class ByteBufferPool
{
    private ConcurrentQueue<ByteBuffer> _byteBufferQueue = new ConcurrentQueue<ByteBuffer>();

    public ByteBuffer Rent(int size)
    {
        if (_byteBufferQueue.TryDequeue(out var byteBuffer))
        {
            if (byteBuffer.Capacity < size)
            {
                byteBuffer.Resize(size);
            }
            byteBuffer.SetReadIndex(0);
            byteBuffer.SetWriteIndex(0);
            return byteBuffer;
        }

        return new ByteBuffer(size);
    }

    public void Return(ByteBuffer byteBuffer)
    {
        _byteBufferQueue.Enqueue(byteBuffer);
    }
}