using System.Buffers;
using System.Net.Sockets;
using Log;

namespace Network;

/// <summary>
/// 封裝 NetworkListener NetworkConnector 共用方法
/// </summary>
public abstract class NetworkBase
{
    public Action<MessagePack> OnReceivedMessage;

    public Action<Socket> OnClosed;

    #region - Receive -
    
    protected abstract void OnReceive(object sender, SocketAsyncEventArgs args);
    
    protected void ReceiveAsync(SocketAsyncEventArgs args)
    {
        try
        {
            var socket = args.AcceptSocket;
            if (socket == null)
            {
                Logger.Error("Receive Failed, client socket is null");
                return;
            }
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
    
    protected bool ReadDataToBuffer(SocketAsyncEventArgs args, ByteBuffer readBuffer)
    {
        var receiveCount = args.BytesTransferred;
        var isNotSuccess = args.SocketError != SocketError.Success;

        if (receiveCount <= 0 || isNotSuccess)
        {
            return false;
        }

        readBuffer.Write(args.Buffer, args.Offset, receiveCount);

        return true;
    }
    
    #endregion

    #region - Send -

    protected abstract void OnSend(object sender, SocketAsyncEventArgs args);
    
    private void SendAsync(SocketAsyncEventArgs args)
    {
        try
        {
            var targetSocket = args.AcceptSocket;
            if (targetSocket == null)
            {
                Logger.Error("Send Failed, client socket is null");
                return;
            }
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
    
    protected void AddMessageToSendQueue(UInt16 messageId, byte[] message, Queue<ByteBuffer> sendQueue, SocketAsyncEventArgs args)
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
            var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
            args.SetBuffer(args.Offset, copyCount);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, copyCount);
            SendAsync(args);
        }
    }
    
    protected void CheckSendQueue(SocketAsyncEventArgs args, Queue<ByteBuffer> sendQueue)
    {
        var count = args.BytesTransferred;

        ByteBuffer byteBuffer;
        lock (sendQueue)
        {
            byteBuffer = sendQueue.First();
        }

        byteBuffer.SetReadIndex(byteBuffer.ReadIndex + count);

        // 完整發送完一個ByteBuffer的資料
        if (byteBuffer.Length <= 0)
        {
            lock (sendQueue)
            {
                sendQueue.Dequeue();
                if (sendQueue.Count >= 1)
                {
                    byteBuffer = sendQueue.First();
                }
                else
                {
                    byteBuffer = null;
                }
            }
        }

        if (byteBuffer != null)
        {
            // SendQueue還有資料，繼續發送
            var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
            args.SetBuffer(args.Offset, copyCount);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, copyCount);
            SendAsync(args);
        }
    }

    #endregion

    #region - Message -

    /// <summary>
    /// | 總長度 2 Byte | MessageId 2 Byte | 資料內容 x Byte |
    /// </summary>
    protected static bool TryUnpackMessage(ByteBuffer byteBuffer, out MessagePack messagePack)
    {
        messagePack = new MessagePack();

        // 連表示總長度的 2 Byte都沒收到
        if (byteBuffer.Length <= 2) return false;
        
        // 資料不完整
        var totalLength = byteBuffer.CheckUInt16();
        if (byteBuffer.Length < totalLength) return false;

        // 資料完整，開始解析
        totalLength = byteBuffer.ReadUInt16();
        var bodyLength = totalLength - 2 - 2;
        
        messagePack.MessageLength = bodyLength;
        messagePack.MessageId     = byteBuffer.ReadUInt16();
        messagePack.Message       = ArrayPool<byte>.Shared.Rent(totalLength);
        byteBuffer.Read(messagePack.Message, 0, bodyLength);

        return true;
    }

    private static void PackMessage(ByteBuffer byteBuffer, UInt16 messageId, byte[] message)
    {
        var totalLength = message.Length + 2 + 2;
        byteBuffer.WriteUInt16((ushort)totalLength);
        byteBuffer.WriteUInt16(messageId);
        byteBuffer.Write(message, 0, message.Length);
    }

    #endregion
}