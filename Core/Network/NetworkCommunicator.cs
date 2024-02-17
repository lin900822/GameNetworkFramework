using System.Net.Sockets;
using Core.Common;
using Core.Logger;

namespace Core.Network;

public class NetworkCommunicator
{
    private bool _isNeedCheckOverReceived    = false;
    private int  _receivedCount              = 0;
    private long _lastResetReceivedCountTime = 0;

    public Socket Socket { get; private set; }

    public Action<ReceivedMessageInfo> OnReceivedMessage;
    public Action<NetworkCommunicator> OnReceivedNothing;

    private readonly ByteBuffer        _receiveBuffer;
    private readonly Queue<ByteBuffer> _sendQueue;

    private readonly SocketAsyncEventArgs _receiveArgs;
    private readonly SocketAsyncEventArgs _sendArgs;

    // Dependencies
    private ByteBufferPool _byteBufferPool;

    // Const
    private const int MaxReceivedCountPerSecond = 500;
    private const int OneSecond                 = 1000;


    private const int ShortPacketLength = 2;
    private const int LongPacketLength  = 4;
    private const int MessageIdLength   = 2;
    private const int RequestIdLength   = 2;

    private const int WarningPacketSize = 1024 * 4;
    private const int MaxPacketSize     = (int)(uint.MaxValue >> 2);

    private const uint LongPacketFlag = 0b_00000000_00000000_10000000_00000000;
    private const uint RequestFlag    = 0b_00000000_00000000_01000000_00000000;

    public NetworkCommunicator(ByteBufferPool pool, int bufferSize)
    {
        _byteBufferPool = pool;

        var receiveArg = new SocketAsyncEventArgs();
        var sendArg    = new SocketAsyncEventArgs();
        receiveArg.SetBuffer(new byte[bufferSize], 0, bufferSize);
        sendArg.SetBuffer(new byte[bufferSize], 0, bufferSize);

        _receiveBuffer = new ByteBuffer(bufferSize);
        _sendQueue     = new Queue<ByteBuffer>();
        _receiveArgs   = receiveArg;
        _sendArgs      = sendArg;
    }

    public virtual void Init(Socket socket, bool isNeedCheckOverReceived = false)
    {
        Socket                    = socket;
        _receiveArgs.AcceptSocket = socket;
        _sendArgs.AcceptSocket    = socket;

        _isNeedCheckOverReceived = isNeedCheckOverReceived;

        _receiveArgs.Completed += OnReceive;
        _sendArgs.Completed    += OnSend;
    }

    public virtual void Release()
    {
        Socket                    = null;
        _receiveArgs.AcceptSocket = null;
        _sendArgs.AcceptSocket    = null;

        _isNeedCheckOverReceived = false;

        lock (_receiveBuffer)
        {
            _receiveBuffer.SetReadIndex(0);
            _receiveBuffer.SetWriteIndex(0);
        }

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
        _sendArgs.Completed    -= OnSend;
    }

    #region - Receive -

    public void ReceiveAsync()
    {
        try
        {
            if (Socket == null)
            {
                Log.Error("Receive Failed, client socket is null");
                return;
            }

            if (!Socket.ReceiveAsync(_receiveArgs))
            {
                OnReceive(this, _receiveArgs);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        if (!ReadDataToReceiveBuffer(args))
        {
            // 收到 0個 Byte代表 Client已關閉
            OnReceivedNothing?.Invoke(this);
            return;
        }

        ParseReceivedData();
        ReceiveAsync();
    }

    private bool ReadDataToReceiveBuffer(SocketAsyncEventArgs args)
    {
        var receiveCount = args.BytesTransferred;
        var isNotSuccess = args.SocketError != SocketError.Success;

        if (receiveCount <= 0 || isNotSuccess)
        {
            return false;
        }

        lock (_receiveBuffer)
        {
            _receiveBuffer.Write(args.Buffer, args.Offset, receiveCount);
        }

        return true;
    }

    private void ParseReceivedData()
    {
        if (!TryUnpackMessage(out var messageInfo))
        {
            return;
        }

        messageInfo.Communicator = this;

        // 分發收到的 Message
        OnReceivedMessage?.Invoke(messageInfo);

        if (IsOverReceived())
        {
            return;
        }

        // 繼續解析 readBuffer
        if (_receiveBuffer.Length > 2)
        {
            ParseReceivedData();
        }
    }

    private bool IsOverReceived()
    {
        if (!_isNeedCheckOverReceived) return false;

        var isOverReceived = false;
        lock (this)
        {
            ++_receivedCount;
            if (_receivedCount >= MaxReceivedCountPerSecond)
            {
                isOverReceived = TimeUtils.GetTimeStamp() - _lastResetReceivedCountTime < OneSecond;

                _receivedCount              = 0;
                _lastResetReceivedCountTime = TimeUtils.GetTimeStamp();
            }
        }

        if (!isOverReceived) return false;

        Log.Warn($"{Socket.RemoteEndPoint} Sent Too Much Packets.");
        OnReceivedNothing?.Invoke(this);
        return true;
    }

    #endregion

    #region - Send -

    public void Send(ushort messageId, byte[] message, bool isRequest = false, ushort requestId = 0)
    {
        if (Socket == null)
        {
            Log.Error("Send Failed, client is null or not connected");
            return;
        }

        if (!Socket.Connected)
        {
            Log.Error("Send Failed, client is null or not connected");
            return;
        }

        AddMessageToSendQueue(messageId, message, isRequest, requestId);
    }

    private void AddMessageToSendQueue(ushort messageId, byte[] message, bool isRequest = false, ushort requestId = 0)
    {
        // 打包資料
        var packetLength = (message.Length > short.MaxValue) ? ShortPacketLength : LongPacketLength;
        var byteBuffer   = _byteBufferPool.Rent(packetLength + MessageIdLength + message.Length);
        PackMessage(byteBuffer, messageId, message, isRequest, requestId);

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
                Log.Error("Send Failed, client socket is null");
                return;
            }

            if (!Socket.SendAsync(_sendArgs))
            {
                OnSend(this, _sendArgs);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (Socket == null)
        {
            Log.Error("OnSend Failed, client socket is null");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            Log.Error($"OnSend Failed, Socket Error: {args.SocketError}");
            return;
        }

        CheckSendQueue();
    }

    private void CheckSendQueue()
    {
        if (_sendQueue.Count <= 0) return;

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

    #region - Handle Packet -

    /// <summary>
    /// 短封包:
    /// | 總長度 2 Byte | MessageId 4 Byte | 資料內容 x Byte |
    /// 
    /// 長封包:
    /// | 總長度 4 Byte | MessageId 4 Byte | 資料內容 x Byte |
    /// 
    /// Request:
    /// | 總長度 4 Byte | MessageId 4 Byte | RequestId 2 Byte | 資料內容 x Byte |
    /// </summary>
    private bool TryUnpackMessage(out ReceivedMessageInfo receivedMessageInfo)
    {
        receivedMessageInfo = new ReceivedMessageInfo();

        // 連表示總長度的 2 Byte都沒收到
        if (_receiveBuffer.Length < ShortPacketLength) return false;

        // 檢查是否是長封包
        var isLongPacket = false;
        var isRequest    = false;
        var totalLength  = (int)_receiveBuffer.CheckUInt16();
        if (HasLongPacketFlag(totalLength))
        {
            if (_receiveBuffer.Length < LongPacketLength) return false;

            isLongPacket = true;
            isRequest    = HasRequestFlag(totalLength);

            totalLength = (int)(totalLength & ~LongPacketFlag);
            totalLength = (int)(totalLength & ~RequestFlag);
            totalLength = (totalLength << 16) | _receiveBuffer.CheckUInt16(2);
        }

        // 資料不完整
        if (_receiveBuffer.Length < totalLength) return false;

        // 資料完整，開始解析
        lock (_receiveBuffer)
        {
            if (isLongPacket)
            {
                totalLength = (int)(_receiveBuffer.ReadUInt16() & ~LongPacketFlag);
                totalLength = (int)(totalLength & ~RequestFlag);
                totalLength = (totalLength << 16) | _receiveBuffer.ReadUInt16();
            }
            else
            {
                totalLength = _receiveBuffer.ReadUInt16();
            }

            var bodyLength = totalLength - (isLongPacket ? LongPacketLength : ShortPacketLength) - MessageIdLength;

            if (isRequest)
            {
                receivedMessageInfo.IsRequest = true;
                receivedMessageInfo.RequestId = _receiveBuffer.ReadUInt16();

                bodyLength -= RequestIdLength;
            }

            receivedMessageInfo.MessageLength = bodyLength;
            receivedMessageInfo.MessageId     = _receiveBuffer.ReadUInt16();
            receivedMessageInfo.Allocate(totalLength);
            _receiveBuffer.Read(receivedMessageInfo.Message, 0, bodyLength);
        }

        return true;
    }

    /// <summary>
    /// 10000000 00000000 => 定義第一個Bit如果是1的話代表長封包, 0代表短封包
    /// </summary>
    private static bool HasLongPacketFlag(int value) => value > short.MaxValue;

    /// <summary>
    /// 01000000 00000000 => 定義第二個Bit如果是1的話代表是Request, 0代表普通的Message
    /// </summary>
    private static bool HasRequestFlag(int value) => (value & RequestFlag) > 0;

    private static void PackMessage(ByteBuffer byteBuffer,        ushort messageId, byte[] message,
        bool                                   isRequest = false, ushort requestId = 0)
    {
        var bodyLength = message.Length;
        if (bodyLength >= MaxPacketSize)
            throw new Exception($"MessageId({messageId}) length({bodyLength}) is over size.");
        if (bodyLength >= WarningPacketSize)
            Log.Warn($"MessageId({messageId}) length({bodyLength}) is too big.");

        int totalLength;

        if (bodyLength > short.MaxValue || isRequest)
        {
            ushort upperTwoByte;
            ushort lowerTwoByte;

            if (isRequest)
            {
                totalLength = LongPacketLength + MessageIdLength + RequestIdLength + bodyLength;

                upperTwoByte = (ushort)((totalLength >> 16) | LongPacketFlag);
                upperTwoByte = (ushort)(upperTwoByte | RequestFlag);
                lowerTwoByte = (ushort)totalLength;

                byteBuffer.WriteUInt16(upperTwoByte);
                byteBuffer.WriteUInt16(lowerTwoByte);
                byteBuffer.WriteUInt16(requestId);
            }
            else
            {
                totalLength = LongPacketLength + MessageIdLength + bodyLength;

                upperTwoByte = (ushort)((totalLength >> 16) | LongPacketFlag);
                lowerTwoByte = (ushort)totalLength;

                byteBuffer.WriteUInt16(upperTwoByte);
                byteBuffer.WriteUInt16(lowerTwoByte);
            }
        }
        else
        {
            totalLength = ShortPacketLength + MessageIdLength + bodyLength;
            byteBuffer.WriteUInt16((ushort)totalLength);
        }

        byteBuffer.WriteUInt16(messageId);
        byteBuffer.Write(message, 0, bodyLength);
    }

    #endregion
}