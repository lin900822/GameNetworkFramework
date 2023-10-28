using System.Net;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkListener : NetworkBase
{
    public int ConnectionCount => _sessionList.Count;

    // Variables
    private Socket _listenFd;

    private int _maxConnectionCount;

    public  Dictionary<Socket, NetworkSession> SessionList => _sessionList;
    private Dictionary<Socket, NetworkSession> _sessionList;

    private NetworkSessionPool _sessionPool;

    public NetworkListener(int maxConnectionCount)
    {
        _maxConnectionCount = maxConnectionCount;
        
        _sessionPool = new NetworkSessionPool();
        _sessionList = new Dictionary<Socket, NetworkSession>();
    }

    public void Listen(string ip, int port)
    {
        var ipAddress = IPAddress.Parse(ip);
        var endPoint  = new IPEndPoint(ipAddress, port);

        _listenFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            _listenFd.Bind(endPoint);
            _listenFd.Listen(_maxConnectionCount);
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
    
    #region - Accept -

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

    private void OnAccept(object sender, SocketAsyncEventArgs acceptEventArg)
    {
        Socket clientFd = acceptEventArg.AcceptSocket;
        if (clientFd == null)
        {
            acceptEventArg.AcceptSocket = null;
            AcceptAsync(acceptEventArg);
            return;
        }

        Logger.Info($"A Client {clientFd.RemoteEndPoint?.ToString()} Connected!");

        // 加入Clients列表
        var client = _sessionPool.Rent();;

        if (client == null)
        {
            CloseSocket(clientFd);
        }
        else
        {
            client.Socket                   =  clientFd;
            client.ReceiveArgs.AcceptSocket =  clientFd;
            client.SendArgs.AcceptSocket    =  clientFd;
            client.ReceiveArgs.Completed    += OnReceive;
            client.SendArgs.Completed       += OnSend;

            lock (_sessionList)
            {
                _sessionList.Add(clientFd, client);
            }

            // 開始接收clientFd傳來的訊息
            ReceiveAsync(client.ReceiveArgs);
        }
        
        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
    }
    
    #endregion
    
    #region - Receive -

    protected override void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        if (!_sessionList.TryGetValue(args.AcceptSocket, out var client))
        {
            Close(args.AcceptSocket);
            Logger.Error("OnReceive Error: Cannot find session");
            return;
        }
        
        if (!ReadDataToBuffer(args, client.ReceiveBuffer))
        {
            // 收到 0個 Byte代表 Client已關閉
            Close(args.AcceptSocket);
            return;
        }

        ParseReceivedData(client);
        ReceiveAsync(args);
    }

    private void ParseReceivedData(NetworkSession session)
    {
        var readBuffer = session.ReceiveBuffer;

        if (!TryUnpackMessage(readBuffer, out var messageId, out var message))
        {
            return;
        }

        // 分發收到的 Message
        OnReceivedMessage?.Invoke(session, messageId, message);

        // 繼續解析 readBuffer
        if (readBuffer.Length > 2)
        {
            ParseReceivedData(session);
        }
    }
    
    #endregion
    
    #region - Send -

    public void SendAll(UInt16 messageId, byte[] message)
    {
        lock (_sessionList)
        {
            using var enumerator = _sessionList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Send(enumerator.Current.Value, messageId, message);
            }
        }
    }

    public void Send(NetworkSession session, UInt16 messageId, byte[] message)
    {
        if (session == null || session.Socket == null || !session.Socket.Connected)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }

        var sendArgs = session.SendArgs;
        AddMessageToSendQueue(messageId, message, session.SendQueue, sendArgs);
    }

    protected override void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (!_sessionList.TryGetValue(args.AcceptSocket, out var client))
        {
            Logger.Error("OnSend Failed, client is null");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            Logger.Error($"OnSend Failed, Socket Error: {args.SocketError}");
            return;
        }

        CheckSendQueue(args, client.SendQueue);
    }
    
    #endregion
    
    private void Close(Socket socket)
    {
        if (!_sessionList.TryGetValue(socket, out var session))
        {
            Logger.Error($"Close Socket Error: Cannot find session");
            return;
        }
        
        RemoveFromSessionList();
        ReturnSession();
        CloseConnection();
        return;

        void RemoveFromSessionList()
        {
            lock (_sessionList)
            {
                if (_sessionList.ContainsKey(socket)) _sessionList.Remove(socket);
            }
        }

        void ReturnSession()
        {
            session.ReceiveArgs.Completed -= OnReceive;
            session.SendArgs.Completed    -= OnSend;
            _sessionPool.Return(session);
        }

        void CloseConnection()
        {
            OnClosed?.Invoke(socket);
            var socketEndPointStr = socket.RemoteEndPoint?.ToString();
            CloseSocket(socket);
            Logger.Info($"{socketEndPointStr} Closed!");
        }
    }

    private void CloseSocket(Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Send);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }

        socket.Close();
    }

    public void Debug()
    {
        _sessionPool.Debug();
    }
}