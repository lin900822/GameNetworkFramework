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
                Logger.LogInfo("Start Listening...");

                var acceptEventArg = new SocketAsyncEventArgs(); // 所有Accept共用這個eventArgs
                acceptEventArg.Completed += OnAccept;

                if (!_listenFd.AcceptAsync(acceptEventArg))
                {
                    OnAccept(this, acceptEventArg);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
            }
        }
    }

    private void OnAccept(object sender, SocketAsyncEventArgs acceptEventArg)
    {
        Socket clientFd = acceptEventArg.AcceptSocket;
        Logger.LogInfo($"A Client {clientFd.RemoteEndPoint.ToString()} Connected!");

        // 加入Clients列表
        var client = new TCPClient();
        client.Socket = clientFd;
        _clients.Add(clientFd, client);

        // 開始接收clientFd傳來的訊息
        var e = _eventArgsPool.Get();
        e.Completed += OnReceive;

        e.AcceptSocket = clientFd;
        if (!clientFd.ReceiveAsync(e))
        {
            OnReceive(this, e);
        }

        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        if (!_listenFd.AcceptAsync(acceptEventArg))
        {
            OnAccept(this, acceptEventArg);
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

        var receivedMsg = Encoding.Unicode.GetString(args.Buffer, args.Offset, receiveCount);
        Logger.LogInfo($"Receive {args.AcceptSocket.RemoteEndPoint} : {receivedMsg}");

        foreach (var client in _clients)
        {
            var sendMsg = $"{args.AcceptSocket.RemoteEndPoint} : {receivedMsg}";
            Send(client.Key, sendMsg);
        }

        if (!args.AcceptSocket.ReceiveAsync(args))
        {
            OnReceive(this, args);
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
            Logger.LogError(e.ToString());
        }

        socket.Close();
        args.Completed -= OnReceive;
        _eventArgsPool.Return(args);

        Logger.LogInfo($"{socketEndPointStr} Closed!");
    }
    
    private void Send(Socket targetSocket, string message)
    {
        var e = _eventArgsPool.Get();
        e.Completed += OnSend;

        var sendData = Encoding.Unicode.GetBytes(message);
        e.SetBuffer(e.Offset, sendData.Length);
        for (int i = 0; i < sendData.Length; i++)
        {
            e.Buffer[e.Offset + i] = sendData[i];
        }

        if (!targetSocket.SendAsync(e))
        {
            OnSend(this, e);
        }
    }

    private void OnSend(object sender, SocketAsyncEventArgs args)
    {
        args.Completed -= OnSend;
        _eventArgsPool.Return(args);
    }
}