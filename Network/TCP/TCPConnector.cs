using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPConnector
{
    private static readonly int _argsBufferSize = 1024 * 10;
    
    private Socket _connectFd;
    private ByteBuffer _receiveBuffer = new ByteBuffer(1024);
    private Queue<ByteBuffer> _sendQueue = new Queue<ByteBuffer>();
    
    private SocketAsyncEventArgs _recieveArgs;
    private SocketAsyncEventArgs _sendArgs;

    public TCPConnector()
    {
        _recieveArgs = new SocketAsyncEventArgs();
        _recieveArgs.SetBuffer(new byte[_argsBufferSize], 0, _argsBufferSize);
        _recieveArgs.Completed += OnReceive;
        
        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.SetBuffer(new byte[_argsBufferSize], 0, _argsBufferSize);
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

    public void Send(string message)
    {
        var data = Encoding.Unicode.GetBytes(message);
        
        var buffer = new ByteBuffer(1024);
        MessageUtils.SetMessage(buffer, 0, data);
        
        _connectFd.Send(buffer.RawData, buffer.ReadIndex, buffer.Length, SocketFlags.None);
    }

    public void Close()
    {
        _connectFd.Shutdown(SocketShutdown.Both);
        _connectFd.Close();
    }
}