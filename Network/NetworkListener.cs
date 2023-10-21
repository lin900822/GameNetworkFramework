using System.Net;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkListener : NetworkBase
{
    public int ConnectionCount => _clients.Count;

    // Variables
    private Socket _listenFd;

    private int _maxConnectionCount;

    public  Dictionary<Socket, NetworkClient> Clients => _clients;
    private Dictionary<Socket, NetworkClient> _clients;

    private SocketAsyncEventArgsPool _eventArgsPool;

    public NetworkListener(int maxConnectionCount)
    {
        _maxConnectionCount = maxConnectionCount;
        
        _eventArgsPool = new SocketAsyncEventArgsPool(maxConnectionCount * 2, EventArgsBufferSize);
        _clients       = new Dictionary<Socket, NetworkClient>();
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
        var client = new NetworkClient();
        client.Socket = clientFd;

        var receiveArgs = _eventArgsPool.Get();
        receiveArgs.Completed    += OnReceive;
        receiveArgs.AcceptSocket =  clientFd;
        client.ReceiveArgs       =  receiveArgs;

        var sendArgs = _eventArgsPool.Get();
        sendArgs.Completed    += OnSend;
        sendArgs.AcceptSocket =  clientFd;
        client.SendArgs       =  sendArgs;

        lock (_clients)
        {
            _clients.Add(clientFd, client);
        }

        // 開始接收clientFd傳來的訊息
        ReceiveAsync(receiveArgs);

        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
    }
    
    #endregion
    
    #region - Receive -

    protected override void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        if (!_clients.TryGetValue(args.AcceptSocket, out var client))
        {
            Close(args.AcceptSocket);
            Logger.Error("OnReceive Error: Cannot find client");
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

    private void ParseReceivedData(NetworkClient client)
    {
        var readBuffer = client.ReceiveBuffer;

        if (!TryUnpackMessage(readBuffer, out var messageId, out var message))
        {
            return;
        }

        // 分發收到的 Message
        OnReceivedMessage?.Invoke(client, messageId, message);

        // 繼續解析 readBuffer
        if (readBuffer.Length > 2)
        {
            ParseReceivedData(client);
        }
    }
    
    #endregion
    
    #region - Send -

    public void SendAll(UInt16 messageId, byte[] message)
    {
        lock (_clients)
        {
            using var enumerator = _clients.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Send(enumerator.Current.Value, messageId, message);
            }
        }
    }

    public void Send(NetworkClient client, UInt16 messageId, byte[] message)
    {
        if (client == null || client.Socket == null || !client.Socket.Connected)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }

        var sendArgs = client.SendArgs;
        AddMessageToSendQueue(messageId, message, client.SendQueue, sendArgs);
    }

    protected override void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (!_clients.TryGetValue(args.AcceptSocket, out var client))
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
        if (!_clients.TryGetValue(socket, out var client))
        {
            Logger.Error($"Close Socket Error: Cannot find client");
            return;
        }
        
        ReturnEventArgs();
        RemoveFromClientList();
        CloseConnection();
        return;

        void ReturnEventArgs()
        {
            var receiveArgs = client.ReceiveArgs;
            var sendArgs    = client.SendArgs;
            receiveArgs.Completed -= OnReceive;
            sendArgs.Completed    -= OnSend;
            _eventArgsPool.Return(receiveArgs);
            _eventArgsPool.Return(sendArgs);
        }

        void RemoveFromClientList()
        {
            lock (_clients)
            {
                if (_clients.ContainsKey(socket)) _clients.Remove(socket);
            }
        }

        void CloseConnection()
        {
            OnClosed?.Invoke(socket);
            
            var socketEndPointStr = socket.RemoteEndPoint?.ToString();
            
            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            socket.Close();
            
            Logger.Info($"{socketEndPointStr} Closed!");
        }
    }

    public void Debug()
    {
        _eventArgsPool.Debug();
    }
}