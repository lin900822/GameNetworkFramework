using System.Net;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkConnector : NetworkBase
{
    public bool IsConnected => _connectFd.Connected;
    
    private Socket _connectFd;
    private ByteBuffer _receiveBuffer = new ByteBuffer(EventArgsBufferSize);
    private Queue<ByteBuffer> _sendQueue = new Queue<ByteBuffer>();

    private SocketAsyncEventArgs _recieveArgs;
    private SocketAsyncEventArgs _sendArgs;

    public NetworkConnector()
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

            ReceiveAsync(_recieveArgs);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    #region - Receive -
    
    protected override void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        if (!ReadDataToBuffer(args, _receiveBuffer))
        {
            Close();
            return;
        }

        ParseReceivedData();
        ReceiveAsync(args);
    }

    private void ParseReceivedData()
    {
        if (!TryUnpackMessage(_receiveBuffer, out var messageId, out var message))
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
    
    #endregion

    #region - Send -
    
    public void Send(UInt16 messageId, byte[] message)
    {
        if (_connectFd == null || !_connectFd.Connected)
        {
            Logger.Error("Send Failed, _connectFd is null or not connected");
            return;
        }
        
        AddMessageToSendQueue(messageId, message, _sendQueue, _sendArgs);
    }

    protected override void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (_connectFd == null || !_connectFd.Connected)
        {
            Logger.Error("Send Failed, _connectFd is null or not connected");
            return;
        }
        
        if (args.SocketError != SocketError.Success)
        {
            Logger.Error($"Send Failed, Socket Error: {args.SocketError}");
            return;
        }
        
        CheckSendQueue(args, _sendQueue);
    }
    
    #endregion

    public void Close()
    {
        OnClosed?.Invoke(_connectFd);
        
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