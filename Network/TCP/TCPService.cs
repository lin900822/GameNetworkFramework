using System.Net.Sockets;
using Log;


namespace Network.TCP;

public class TCPService
{
    // Define
    protected static readonly int EventArgsBufferSize = 1024 * 50;
    protected static readonly int DefaultPoolCapacity = 10000;

    public Action<TCPClient, UInt16, byte[]> OnReceivedMessage;

    protected virtual void OnReceive(object sender, SocketAsyncEventArgs args)
    {
    }
    
    protected virtual void OnSend(object sender, SocketAsyncEventArgs args)
    {
    }

    protected void ReceiveAsync(SocketAsyncEventArgs args)
    {
        try
        {
            var socket = args.AcceptSocket;
            if (!socket.ReceiveAsync(args))
            {
                OnReceive(this, args);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }
    
    protected void SendAsync(SocketAsyncEventArgs args)
    {
        try
        {
            var targetSocket = args.AcceptSocket;
            if (!targetSocket.SendAsync(args))
            {
                OnSend(this, args);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }
    
    protected void InnerSend(UInt16 messageId, byte[] message, Queue<ByteBuffer> sendQueue, SocketAsyncEventArgs args)
    {
        // 打包資料
        var byteBuffer = new ByteBuffer(2 + 2 + message.Length);
        PackMessage(byteBuffer, messageId, message);

        // 透過 SendQueue處理發送不完整問題
        int count = 0;
        lock (sendQueue)
        {
            sendQueue.Enqueue(byteBuffer);
            count = sendQueue.Count;
        }

        // 當 SendQueue只有 1個時發送
        // SendQueue.Count > 1時, 在 OnSend()裡面會持續發送, 直到發送完
        if (count == 1)
        {
            args.SetBuffer(args.Offset, byteBuffer.Length);

            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, byteBuffer.Length);
            SendAsync(args);
        }
    }
    
    /// <summary>
    /// | 總長度 2 Byte | MessageId 2 Byte | 資料內容 x Byte |
    /// </summary>
    protected bool TryParseMessage(ByteBuffer byteBuffer, out UInt16 outMessageId, out byte[] outMessage)
    {
        outMessageId = 0;
        outMessage = null;
        
        // 連表示總長度的 2 Byte都沒收到
        if (byteBuffer.Length <= 2) return false;
        
        // 資料不完整
        var totalLength = byteBuffer.CheckUInt16();
        if (byteBuffer.Length < totalLength) return false;

        // 資料完整，開始解析
        totalLength = byteBuffer.ReadUInt16();
        outMessageId = byteBuffer.ReadUInt16();

        var bodyLength = totalLength - 2 - 2;

        outMessage = new byte[bodyLength];
        byteBuffer.Read(outMessage, 0, bodyLength);

        return true;
    }

    protected void PackMessage(ByteBuffer byteBuffer, UInt16 messageId, byte[] message)
    {
        var totalLength = message.Length + 2 + 2;
        byteBuffer.WriteUInt16((ushort)totalLength);
        byteBuffer.WriteUInt16(messageId);
        byteBuffer.Write(message, 0, message.Length);
    }
}