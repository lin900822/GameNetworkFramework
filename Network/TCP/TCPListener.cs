using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

/// <summary>
/// TODO
/// 1. 黏包拆包處理
/// 2. 發送不完整處理
/// 3. Message Router
/// 4. SocketAsyncEventArgs, Buffer重複利用
/// 5. 大小端處理
/// </summary>
public class TCPListener
{
    // Define
    private static readonly int EVENTARGS_BUFFER_SIZE = 1024;
    private static readonly int DEFAULT_POOL_CAPACITY = 1;

    // Variables
    private Socket _listenFd;

    private Dictionary<Socket, TCPClient> _clients;

    // Utils
    private SocketAsyncEventArgsPool _eventArgsPool;

    public void Start(string ip, int port)
    {
        InitComponents();
        InitListenFd();

        // Local Methods

        void InitComponents()
        {
            _eventArgsPool = new SocketAsyncEventArgsPool(DEFAULT_POOL_CAPACITY, EVENTARGS_BUFFER_SIZE);
            _clients       = new Dictionary<Socket, TCPClient>();
        }

        void InitListenFd()
        {
            var ipAddress = IPAddress.Parse(ip);
            var endPoint  = new IPEndPoint(ipAddress, port);

            _listenFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _listenFd.Bind(endPoint);
                _listenFd.Listen();
                Logger.Info("Start Listening...");

                var acceptEventArg = new SocketAsyncEventArgs(); // 所有Accept共用這個eventArgs
                acceptEventArg.Completed += OnAccept;

                AcceptAsync(acceptEventArg);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }

    private void OnAccept(object sender, SocketAsyncEventArgs acceptEventArg)
    {
        Socket clientFd = acceptEventArg.AcceptSocket;
        Logger.Info($"A Client {clientFd.RemoteEndPoint.ToString()} Connected!");

        // 加入Clients列表
        var client = new TCPClient();
        client.Socket = clientFd;
        _clients.Add(clientFd, client);

        // 開始接收clientFd傳來的訊息
        var e = _eventArgsPool.Get();
        e.Completed += OnReceive;

        e.AcceptSocket = clientFd;
        ReceiveAsync(e);

        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
    }

    private void AcceptAsync(SocketAsyncEventArgs acceptEventArg)
    {
        try
        {
            if (!_listenFd.AcceptAsync(acceptEventArg))
            {
                OnAccept(this, acceptEventArg);
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
            var clientFd = args.AcceptSocket;
            if (!clientFd.ReceiveAsync(args))
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
        var isNotSuccess = args.SocketError != SocketError.Success;
        if (receiveCount <= 0 || isNotSuccess)
        {
            Close(args);
            return;
        }

        var client     = _clients[args.AcceptSocket];
        var readBuffer = client.ReadBuffer;

        readBuffer.Write(args.Buffer, args.Offset, receiveCount);

        ParseReceivedData(client);

        ReceiveAsync(args);
    }

    private void ParseReceivedData(TCPClient client)
    {
        var readBuffer = client.ReadBuffer;
        
        if (!MessageUtils.TryParse(readBuffer, out var messageId, out var message))
        {
            return;
        }
        
        // 分發收到的Message
        string msg = Encoding.Unicode.GetString(message);
        Logger.Info(msg);

        foreach (var c in _clients)
        {
            Send(c.Value.Socket, msg);
        }
        
        // 繼續解析 readBuffer
        if (readBuffer.Length > 2)
        {
            ParseReceivedData(client);
        }
    }

    private void Close(SocketAsyncEventArgs args)
    {
        var socket            = args.AcceptSocket;
        var socketEndPointStr = socket.RemoteEndPoint.ToString();

        if (_clients.ContainsKey(socket)) _clients.Remove(socket);

        try
        {
            socket.Shutdown(SocketShutdown.Send);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }

        socket.Close();
        args.Completed -= OnReceive;
        _eventArgsPool.Return(args);

        Logger.Info($"{socketEndPointStr} Closed!");
    }

    private void Send(Socket targetSocket, string message)
    {
        var e = _eventArgsPool.Get();
        e.Completed += OnSend;
        
        var data = Encoding.Unicode.GetBytes(message);
        var buffer = new ByteBuffer(1024);
        MessageUtils.SetMessage(buffer, 0, data);

        e.SetBuffer(e.Offset, buffer.Length);
        for (int i = buffer.ReadIndex; i < buffer.Length; i++)
        {
            e.Buffer[e.Offset + i] = buffer.RawData[i];
        }

        e.AcceptSocket = targetSocket;
        SendAsync(e);
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
        args.Completed -= OnSend;
        _eventArgsPool.Return(args);
    }
}