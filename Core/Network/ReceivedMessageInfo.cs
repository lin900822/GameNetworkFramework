using System.Buffers;
using Core.Logger;
using Google.Protobuf;

namespace Core.Network;

public struct ReceivedMessageInfo
{
    /// <summary>
    /// 訊息Id
    /// </summary>
    public ushort MessageId;

    /// <summary>
    /// 是否是Request
    /// </summary>
    public bool IsRequest;
    
    /// <summary>
    /// 請求Id
    /// </summary>
    public ushort RequestId;
    
    /// <summary>
    /// 訊息本體
    /// </summary>
    public ByteBuffer Message;

    public bool TryDecode<T>(out T outMessage) where T : IMessage, new()
    {
        try
        { 
            outMessage = new T();
            outMessage.MergeFrom(Message.RawData, Message.ReadIndex, Message.Length);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            outMessage = default(T);
            return false;
        }
    }

    public void Allocate(int size)
    {
        Message = ByteBufferPool.Shared.Rent(size);
    }

    public void Release()
    {
        ByteBufferPool.Shared.Return(Message);
    }
}