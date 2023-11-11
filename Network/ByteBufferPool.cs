using System.Collections.Concurrent;

namespace Network;

public class ByteBufferPool
{
    private ConcurrentQueue<ByteBuffer> _byteBufferQueue = new ConcurrentQueue<ByteBuffer>();

    public ByteBuffer Rent()
    {
        if (_byteBufferQueue.TryDequeue(out var byteBuffer))
        {
            byteBuffer.SetReadIndex(0);
            byteBuffer.SetWriteIndex(0);
            return byteBuffer;
        }

        return null;
    }

    public void Return(ByteBuffer byteBuffer)
    {
        _byteBufferQueue.Enqueue(byteBuffer);
    }
}