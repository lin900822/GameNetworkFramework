using System.Net;
using System.Net.Sockets;
using Core.Log;
using Core.Metrics;

namespace Core.Network;

public class NetworkListener 
{
    public Action<NetworkSession> OnSessionConnected;
    public Action<NetworkSession> OnSessionDisconnected;

    public Action<ReceivedMessageInfo> OnReceivedMessage;
    
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
            Log.Log.Info($"Start Listening at Port: {port}");

            var acceptEventArg = new SocketAsyncEventArgs(); // 所有Accept共用這個eventArgs
            acceptEventArg.Completed += OnAccept;

            AcceptAsync(acceptEventArg);
        }
        catch (Exception e)
        {
            Log.Log.Error(e.Message);
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
            Log.Log.Error(e.ToString());
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

        Log.Log.Info($"A Client {clientFd.RemoteEndPoint?.ToString()} Connected!");

        // 加入SessionList列表
        var session = _sessionPool.Rent();;

        if (session == null)
        {
            // 超過最大連線數
            CloseSocket(clientFd);
        }
        else
        {
            session.SetActive(clientFd);
            session.OnReceivedMessage += HandleReceivedMessage;
            session.OnReceivedNothing += OnCommunicatorReceivedNothing;

            lock (_sessionList)
            {
                _sessionList.Add(clientFd, session);
            }
            
            SystemMetrics.UpdateSessionCount(ConnectionCount);
            
            OnSessionConnected?.Invoke(session);

            // 開始接收clientFd傳來的訊息
            session.ReceiveAsync();
        }
        
        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
    }

    private void HandleReceivedMessage(ReceivedMessageInfo receivedMessageInfo)
    {
        OnReceivedMessage?.Invoke(receivedMessageInfo);
    }
    
    #endregion
    
    #region - Send -

    public void SendAll(uint messageId, byte[] message, uint stateCode = 0)
    {
        lock (_sessionList)
        {
            using var enumerator = _sessionList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Send(enumerator.Current.Value, messageId, message, stateCode);
            }
        }
    }

    public void Send(NetworkCommunicator communicator, uint messageId, byte[] message, uint stateCode = 0)
    {
        if (communicator == null)
        {
            Log.Log.Error("Send Failed, client is null or not connected");
            return;
        }
        
        communicator.Send(messageId, message, stateCode);
    }
    
    #endregion

    private void OnCommunicatorReceivedNothing(NetworkCommunicator _communicator)
    {
        Close(_communicator.Socket);
        SystemMetrics.UpdateSessionCount(ConnectionCount);
    }
    
    public void Close(Socket socket)
    {
        if (!_sessionList.TryGetValue(socket, out var session))
        {
            Log.Log.Error($"Close Socket Error: Cannot find session");
            return;
        }
        
        RemoveFromSessionList();
        ReturnSession();
        CloseConnection();
        return;

        void RemoveFromSessionList()
        {
            OnSessionDisconnected?.Invoke(session);
            lock (_sessionList)
            {
                if (_sessionList.ContainsKey(socket)) _sessionList.Remove(socket);
            }
        }

        void ReturnSession()
        {
            session.OnReceivedMessage -= HandleReceivedMessage;
            session.OnReceivedNothing -= OnCommunicatorReceivedNothing;
            _sessionPool.Return(session);
        }

        void CloseConnection()
        {
            var socketEndPointStr = socket.RemoteEndPoint?.ToString();
            CloseSocket(socket);
            Log.Log.Info($"{socketEndPointStr} Closed!");
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
            Log.Log.Error(e.ToString());
        }

        socket.Close();
    }
}