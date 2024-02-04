using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkCommunicator
{
    public Socket Socket { get; private set; }
    
    public Action<ReceivedMessageInfo> OnReceivedMessage;
    public Action<NetworkCommunicator> OnReceivedNothing;
    
    private readonly ByteBuffer _receiveBuffer;
    private readonly Queue<ByteBuffer> _sendQueue;

    private readonly SocketAsyncEventArgs _receiveArgs;
    private readonly SocketAsyncEventArgs _sendArgs;

    // Dependencies
    private ByteBufferPool _byteBufferPool;

    public NetworkCommunicator(ByteBufferPool pool, int bufferSize)
    {
        _byteBufferPool = pool;
        
        var receiveArg = new SocketAsyncEventArgs();
        var sendArg    = new SocketAsyncEventArgs();
        receiveArg.SetBuffer(new byte[bufferSize], 0, bufferSize);
        sendArg.SetBuffer(new byte[bufferSize], 0, bufferSize);

        _receiveBuffer = new ByteBuffer(bufferSize);
        _sendQueue = new Queue<ByteBuffer>();
        _receiveArgs = receiveArg;
        _sendArgs = sendArg;
    }

    public virtual void SetActive(Socket socket)
    {
        Socket = socket;
        _receiveArgs.AcceptSocket = socket;
        _sendArgs.AcceptSocket = socket;

        _receiveArgs.Completed += OnReceive;
        _sendArgs.Completed += OnSend;
    }

    public virtual void SetInactive()
    {
        Socket = null;
        _receiveArgs.AcceptSocket = null;
        _sendArgs.AcceptSocket    = null;
        
        _receiveBuffer.SetReadIndex(0);
        _receiveBuffer.SetWriteIndex(0);

        lock (_sendQueue)
        {
            var sendQueueCount = _sendQueue.Count;
            for (int i = 0; i < sendQueueCount; i++)
            {
                var item = _sendQueue.Dequeue();
                _byteBufferPool.Return(item);
            }
        }
        
        _receiveArgs.Completed -= OnReceive;
        _sendArgs.Completed -= OnSend;
    }

    #region - Receive -

    public void ReceiveAsync()
    {
        try
        {
            if (Socket == null)
            {
                Logger.Error("Receive Failed, client socket is null");
                return;
            }

            if (!Socket.ReceiveAsync(_receiveArgs))
            {
                OnReceive(this, _receiveArgs);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    private void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        if (!ReadDataToBuffer(args, _receiveBuffer))
        {
            // 收到 0個 Byte代表 Client已關閉
            OnReceivedNothing?.Invoke(this);
            return;
        }

        ParseReceivedData();
        ReceiveAsync();
    }

    private bool ReadDataToBuffer(SocketAsyncEventArgs args, ByteBuffer readBuffer)
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

    private void ParseReceivedData()
    {
        if (!TryUnpackMessage(_receiveBuffer, out var messageInfo))
        {
            return;
        }

        messageInfo.Communicator = this;

        // 分發收到的 Message
        OnReceivedMessage?.Invoke(messageInfo);

        // 繼續解析 readBuffer
        if (_receiveBuffer.Length > 2)
        {
            ParseReceivedData();
        }
    }

    #endregion

    #region - Send -

    public void Send(uint messageId, byte[] message, uint stateCode = 0)
    {
        if (Socket == null)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }

        if (!Socket.Connected)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }

        AddMessageToSendQueue(messageId, stateCode, message);
    }

    private void AddMessageToSendQueue(uint messageId, uint stateCode, byte[] message)
    {
        // 打包資料
        var byteBuffer = _byteBufferPool.Rent(2 + 2 + 4 + message.Length);
        PackMessage(byteBuffer, messageId, stateCode, message);

        // 透過 SendQueue處理發送不完整問題
        int count = 0;
        lock (_sendQueue)
        {
            _sendQueue.Enqueue(byteBuffer);
            count = _sendQueue.Count;
        }

        // 當 SendQueue只有 1個時發送
        // SendQueue.Count > 1時, 在 OnSend()裡面會持續發送, 直到發送完
        if (count == 1)
        {
            var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
            _sendArgs.SetBuffer(_sendArgs.Offset, copyCount);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, _sendArgs.Buffer, _sendArgs.Offset, copyCount);
            SendAsync();
        }
    }

    private void SendAsync()
    {
        try
        {
            if (Socket == null)
            {
                Logger.Error("Send Failed, client socket is null");
                return;
            }

            if (!Socket.SendAsync(_sendArgs))
            {
                OnSend(this, _sendArgs);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    private void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (Socket == null)
        {
            Logger.Error("OnSend Failed, client socket is null");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            Logger.Error($"OnSend Failed, Socket Error: {args.SocketError}");
            return;
        }

        CheckSendQueue();
    }

    private void CheckSendQueue()
    {
        var count = _sendArgs.BytesTransferred;

        ByteBuffer byteBuffer;
        lock (_sendQueue)
        {
            byteBuffer = _sendQueue.First();
        }

        byteBuffer.SetReadIndex(byteBuffer.ReadIndex + count);

        // 完整發送完一個ByteBuffer的資料
        if (byteBuffer.Length <= 0)
        {
            ByteBuffer dequeueBuffer;
            lock (_sendQueue)
            {
                dequeueBuffer = _sendQueue.Dequeue();
                if (_sendQueue.Count >= 1)
                {
                    byteBuffer = _sendQueue.First();
                }
                else
                {
                    byteBuffer = null;
                }
            }

            _byteBufferPool.Return(dequeueBuffer);
        }

        if (byteBuffer != null)
        {
            // SendQueue還有資料，繼續發送
            var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
            _sendArgs.SetBuffer(_sendArgs.Offset, copyCount);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, _sendArgs.Buffer, _sendArgs.Offset, copyCount);
            SendAsync();
        }
    }

    #endregion
    
    /// <summary>
    /// | 總長度 2 Byte | MessageId 4 Byte | State Code 4 Byte | 資料內容 x Byte |
    /// </summary>
    private static bool TryUnpackMessage(ByteBuffer byteBuffer, out ReceivedMessageInfo receivedMessageInfo)
    {
        receivedMessageInfo = new ReceivedMessageInfo();

        // 連表示總長度的 2 Byte都沒收到
        if (byteBuffer.Length <= 2) return false;

        // 資料不完整
        var totalLength = byteBuffer.CheckUInt16();
        if (byteBuffer.Length < totalLength) return false;

        // 資料完整，開始解析
        totalLength = byteBuffer.ReadUInt16();
        var bodyLength = totalLength - 2 - 4 - 4;

        receivedMessageInfo.MessageLength = bodyLength;
        receivedMessageInfo.MessageId = byteBuffer.ReadUInt32();
        receivedMessageInfo.StateCode = byteBuffer.ReadUInt32();
        receivedMessageInfo.Allocate(totalLength);
        byteBuffer.Read(receivedMessageInfo.Message, 0, bodyLength);

        return true;
    }

    private static void PackMessage(ByteBuffer byteBuffer, uint messageId, uint stateCode, byte[] message)
    {
        var totalLength = message.Length + 2 + 4 + 4;
        byteBuffer.WriteUInt16((ushort)totalLength);
        byteBuffer.WriteUInt32(messageId);
        byteBuffer.WriteUInt32(stateCode);
        byteBuffer.Write(message, 0, message.Length);
    }
}