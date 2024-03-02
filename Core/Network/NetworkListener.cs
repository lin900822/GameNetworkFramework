using System.Net;
using System.Net.Sockets;
using Core.Logger;

namespace Core.Network;

public class NetworkListener
{
    public Action<NetworkSession> OnSessionConnected;
    public Action<NetworkSession> OnSessionDisconnected;

    public Action<NetworkCommunicator, ReceivedMessageInfo> OnReceivedMessage;

    public int ConnectionCount => _sessionList.Count;

    // Variables
    private Socket _listenFd;

    private readonly int _maxConnectionCount;

    private readonly Queue<NetworkSession>      _sessionsToAdd;
    private readonly Queue<NetworkCommunicator> _communicatorsToClose;

    public Dictionary<Socket, NetworkSession> SessionList => _sessionList;

    private readonly Dictionary<Socket, NetworkSession> _sessionList;

    private readonly NetworkSessionPool _sessionPool;

    public NetworkListener(int maxConnectionCount)
    {
        _maxConnectionCount = maxConnectionCount;

        _sessionsToAdd        = new Queue<NetworkSession>();
        _communicatorsToClose = new Queue<NetworkCommunicator>();

        _sessionPool = new NetworkSessionPool(_maxConnectionCount);
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
            Log.Info($"Start Listening at Port: {port}");

            var acceptEventArg = new SocketAsyncEventArgs(); // 所有Accept共用這個eventArgs
            acceptEventArg.Completed += OnAccept;

            AcceptAsync(acceptEventArg);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }

    #region - Life Cycle -
    
    // 每個Frame要處理的順序： AddSessions -> HandleMessages -> CloseSessions

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void AddSessions()
    {
        if (_sessionsToAdd.Count <= 0) return;

        lock (_sessionsToAdd)
        {
            foreach (var session in _sessionsToAdd)
            {
                _sessionList.Add(session.Socket, session);

                OnSessionConnected?.Invoke(session);

                // 開始接收clientFd傳來的訊息
                session.ReceiveAsync();
            }

            _sessionsToAdd.Clear();
        }
    }

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void CloseSessions()
    {
        if (_communicatorsToClose.Count <= 0) return;

        lock (_communicatorsToClose)
        {
            foreach (var session in _communicatorsToClose)
            {
                Close(session.Socket);
            }

            _communicatorsToClose.Clear();
        }
    }

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void HandleMessages()
    {
        foreach (var session in SessionList.Values)
        {
            session.HandleMessages();
        }
    }

    #endregion

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
            Log.Error(e.ToString());
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

        //Log.Info($"A Client {clientFd.RemoteEndPoint?.ToString()} Connected!");

        // 加入SessionList列表
        var session = _sessionPool.Rent();

        if (session == null)
        {
            // 超過最大連線數
            CloseSocket(clientFd);
        }
        else
        {
            session.Init(clientFd, true);
            session.OnReceivedMessage += HandleReceivedMessage;
            session.OnClose           += OnCommunicatorClose;

            lock (_sessionsToAdd)
            {
                _sessionsToAdd.Enqueue(session);
            }
        }

        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
    }

    #endregion

    #region - NetworkCommunicator Events -

    private void HandleReceivedMessage(NetworkCommunicator communicator, ReceivedMessageInfo receivedMessageInfo)
    {
        OnReceivedMessage?.Invoke(communicator, receivedMessageInfo);
    }
    
    private void OnCommunicatorClose(NetworkCommunicator communicator)
    {
        lock (_communicatorsToClose)
        {
            _communicatorsToClose.Enqueue(communicator);
        }
    }

    #endregion

    #region - Send -

    public void SendAll(ushort messageId, byte[] message)
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

    public void Send(NetworkCommunicator communicator, ushort messageId, byte[] message)
    {
        if (communicator == null)
        {
            Log.Error("Send Failed, client is null or not connected");
            return;
        }

        communicator.Send(messageId, message);
    }

    #endregion

    #region - Close -

    public void Close(Socket socket)
    {
        if (socket == null) return;
        
        if (!_sessionList.TryGetValue(socket, out var session))
        {
            Log.Error($"Close Socket Error: Cannot find session");
            return;
        }

        RemoveFromSessionList();
        ReturnSession();
        CloseConnection();
        return;

        void RemoveFromSessionList()
        {
            OnSessionDisconnected?.Invoke(session);
            if (_sessionList.ContainsKey(socket)) _sessionList.Remove(socket);
        }

        void ReturnSession()
        {
            session.OnReceivedMessage -= HandleReceivedMessage;
            session.OnClose           -= OnCommunicatorClose;
            _sessionPool.Return(session);
        }

        void CloseConnection()
        {
            var socketEndPointStr = socket.RemoteEndPoint?.ToString();
            CloseSocket(socket);
            //Log.Info($"{socketEndPointStr} Closed!");
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
            Log.Error(e.ToString());
        }

        socket.Close();
    }

    #endregion
}