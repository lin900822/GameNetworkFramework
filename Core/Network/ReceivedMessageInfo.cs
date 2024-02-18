using System.Buffers;
using Core.Logger;
using Google.Protobuf;

namespace Core.Network;

public struct ReceivedMessageInfo
{
    public NetworkCommunicator Communicator;

    public NetworkSession Session
    {
        get
        {
            if (Communicator == null) return null;
            if (Communicator is NetworkSession session) return session;
            return null;
        }
    }

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

    public int MessageLength;
    
    /// <summary>
    /// 訊息本體
    /// </summary>
    public byte[] Message;

    public bool TryDecode<T>(out T outMessage) where T : IMessage, new()
    {
        try
        { 
            outMessage = new T();
            outMessage.MergeFrom(Message, 0, MessageLength);
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
        //Message = ByteBufferPool.Shared.Rent(size);
        Message = ArrayPool<byte>.Shared.Rent(size);
    }

    public void Release()
    {
        //ByteBufferPool.Shared.Return(Message);
        ArrayPool<byte>.Shared.Return(Message);
    }
}