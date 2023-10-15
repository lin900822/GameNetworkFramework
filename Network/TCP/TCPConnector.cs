using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPConnector
{
    private static readonly int EVENTARGS_BUFFER_SIZE = 1024;

    private Socket _connectFd;
    private ByteBuffer _receiveBuffer = new ByteBuffer(1024);
    private Queue<ByteBuffer> _sendQueue = new Queue<ByteBuffer>();

    private SocketAsyncEventArgs _recieveArgs;
    private SocketAsyncEventArgs _sendArgs;

    public TCPConnector()
    {
        _recieveArgs = new SocketAsyncEventArgs();
        _recieveArgs.SetBuffer(new byte[EVENTARGS_BUFFER_SIZE], 0, EVENTARGS_BUFFER_SIZE);
        _recieveArgs.Completed += OnReceive;

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.SetBuffer(new byte[EVENTARGS_BUFFER_SIZE], 0, EVENTARGS_BUFFER_SIZE);
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

            if (_connectFd.Connected)
            {
                Logger.Info("Connected!");

                _recieveArgs.AcceptSocket = _connectFd;
                _sendArgs.AcceptSocket = _connectFd;

                ReceiveAsync(_recieveArgs);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    private void ReceiveAsync(SocketAsyncEventArgs args)
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

    private void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        var receiveCount = args.BytesTransferred;
        _receiveBuffer.Write(args.Buffer, args.Offset, receiveCount);

        ParseReceivedData();

        ReceiveAsync(args);
    }

    private void ParseReceivedData()
    {
        if (!MessageUtils.TryParse(_receiveBuffer, out var messageId, out var message))
        {
            return;
        }

        // 分發收到的Message
        string msg = Encoding.Unicode.GetString(message);
        Logger.Info(msg);

        // 繼續解析 readBuffer
        if (_receiveBuffer.Length > 2)
        {
            ParseReceivedData();
        }
    }

    public void Send(UInt16 messageId, byte[] message)
    {
        // 打包資料
        var byteBuffer = new ByteBuffer(2 + 2 + message.Length);
        MessageUtils.SetMessage(byteBuffer, messageId, message);

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
            // 準備發送用的 SocketAsyncEventArgs
            _sendArgs.SetBuffer(_sendArgs.Offset, byteBuffer.Length);
            _sendArgs.AcceptSocket = _connectFd;
            
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, _sendArgs.Buffer, _sendArgs.Offset, byteBuffer.Length);
            SendAsync(_sendArgs);
        }
    }

    private void SendAsync(SocketAsyncEventArgs args)
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
            SendAsync(args);
        }
    }

    public void Close()
    {
        _connectFd.Shutdown(SocketShutdown.Both);
        _connectFd.Close();
    }
}