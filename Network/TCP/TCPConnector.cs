using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPConnector : TCPService
{
    private Socket _connectFd;
    private ByteBuffer _receiveBuffer = new ByteBuffer(1024);
    private Queue<ByteBuffer> _sendQueue = new Queue<ByteBuffer>();

    private SocketAsyncEventArgs _recieveArgs;
    private SocketAsyncEventArgs _sendArgs;

    public TCPConnector()
    {
        _recieveArgs = new SocketAsyncEventArgs();
        _recieveArgs.SetBuffer(new byte[EventArgsBufferSize], 0, EventArgsBufferSize);
        _recieveArgs.Completed += OnReceive;

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.SetBuffer(new byte[EventArgsBufferSize], 0, EventArgsBufferSize);
        _sendArgs.Completed += OnSend;
    }

    public async void Connect(string ip, int port)
    {
        var ipAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        try
        {
            _connectFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Logger.Info($"Start Connecting to {ip}...");

            await _connectFd.ConnectAsync(ipEndPoint);

            if (!_connectFd.Connected)
            {
                return;
            }
            
            Logger.Info("Connected!");

            _recieveArgs.AcceptSocket = _connectFd;
            _sendArgs.AcceptSocket = _connectFd;

            ReceiveAsync(_recieveArgs, OnReceive);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    private void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        var receiveCount = args.BytesTransferred;
        var isNotSuccess = args.SocketError != SocketError.Success;
        if (receiveCount <= 0 || isNotSuccess)
        {
            Close();
            return;
        }
        
        _receiveBuffer.Write(args.Buffer, args.Offset, receiveCount);

        ParseReceivedData();

        ReceiveAsync(args, OnReceive);
    }

    private void ParseReceivedData()
    {
        if (!TryParseMessage(_receiveBuffer, out var messageId, out var message))
        {
            return;
        }

        // 分發收到的 Message
        OnReceivedMessage?.Invoke(null, messageId, message);

        // 繼續解析 readBuffer
        if (_receiveBuffer.Length > 2)
        {
            ParseReceivedData();
        }
    }

    public void Send(UInt16 messageId, byte[] message)
    {
        InnerSend(messageId, message, _sendQueue, _sendArgs, OnSend);
    }

    private void OnSend(object sender, SocketAsyncEventArgs args)
    {
        var count = args.BytesTransferred;

        ByteBuffer byteBuffer;
        lock (_sendQueue)
        {
            byteBuffer = _sendQueue.First();
        }

        byteBuffer.SetReadIndex(byteBuffer.ReadIndex + count);

        // 完整發送完一個ByteBuffer的資料
        if (byteBuffer.Length == 0)
        {
            lock (_sendQueue)
            {
                _sendQueue.Dequeue();
                if (_sendQueue.Count >= 1)
                {
                    byteBuffer = _sendQueue.First();
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
            _sendArgs.SetBuffer(_sendArgs.Offset, byteBuffer.Length);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, byteBuffer.Length);
            SendAsync(args, OnSend);
        }
    }

    public void Close()
    {
        try
        {
            _connectFd.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
        
        _connectFd.Close();
        Logger.Info("Connection Closed!");
    }
}