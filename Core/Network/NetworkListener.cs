using System.Net;
using System.Net.Sockets;
using Core.Logger;

namespace Core.Network;

public class NetworkListener
{
    public Action<NetworkCommunicator> OnCommunicatorConnected;
    public Action<NetworkCommunicator> OnCommunicatorDisconnected;

    public Action<NetworkCommunicator, ReceivedMessageInfo> OnReceivedMessage;

    public int ConnectionCount => _communicatorList.Count;

    // Variables
    private Socket _listenFd;

    private readonly int _maxConnectionCount;

    private readonly Queue<NetworkCommunicator> _communicatorsToAdd;
    private readonly Queue<NetworkCommunicator> _communicatorsToClose;

    public Dictionary<Socket, NetworkCommunicator> CommunicatorList => _communicatorList;

    private readonly Dictionary<Socket, NetworkCommunicator> _communicatorList;

    private readonly NetworkCommunicatorPool _communicatorPool;

    public NetworkListener(int maxConnectionCount)
    {
        _maxConnectionCount = maxConnectionCount;

        _communicatorsToAdd   = new Queue<NetworkCommunicator>();
        _communicatorsToClose = new Queue<NetworkCommunicator>();

        _communicatorPool = new NetworkCommunicatorPool(_maxConnectionCount);
        _communicatorList = new Dictionary<Socket, NetworkCommunicator>();
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

    // 每個Frame要處理的順序： AddCommunicators -> HandleMessages -> CloseCommunicators

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void AddCommunicators()
    {
        if (_communicatorsToAdd.Count <= 0) return;

        lock (_communicatorsToAdd)
        {
            foreach (var communicator in _communicatorsToAdd)
            {
                _communicatorList.Add(communicator.Socket, communicator);

                OnCommunicatorConnected?.Invoke(communicator);

                // 開始接收clientFd傳來的訊息
                communicator.ReceiveAsync();
            }

            _communicatorsToAdd.Clear();
        }
    }

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void CloseCommunicators()
    {
        if (_communicatorsToClose.Count <= 0) return;

        lock (_communicatorsToClose)
        {
            foreach (var communicator in _communicatorsToClose)
            {
                InnerClose(communicator.Socket);
            }

            _communicatorsToClose.Clear();
        }
    }

    /// <summary>
    /// 需在Main Thread上執行
    /// </summary>
    public void HandleMessages()
    {
        foreach (var communicator in CommunicatorList.Values)
        {
            communicator.HandleMessages();
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

        // 加入CommunicatorList列表
        var communicator = _communicatorPool.Rent();

        if (communicator == null)
        {
            // 超過最大連線數
            CloseSocket(clientFd);
        }
        else
        {
            communicator.Init(clientFd, true);
            communicator.OnReceivedMessage += HandleReceivedMessage;
            communicator.OnClose           += OnCommunicatorClose;

            lock (_communicatorsToAdd)
            {
                _communicatorsToAdd.Enqueue(communicator);
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
        Close(communicator);
    }

    #endregion

    #region - Send -

    public void SendAll(ushort messageId, byte[] message)
    {
        lock (_communicatorList)
        {
            using var enumerator = _communicatorList.GetEnumerator();
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

    public void Close(NetworkCommunicator communicator)
    {
        if (communicator == null)
            return;
        
        lock (_communicatorsToClose)
        {
            _communicatorsToClose.Enqueue(communicator);
        }
    }

    private void InnerClose(Socket socket)
    {
        if (socket == null) return;

        if (!_communicatorList.TryGetValue(socket, out var communicator))
        {
            Log.Error($"Close Socket Error: Cannot find communicator");
            return;
        }

        RemoveFromCommunicatorList();
        ReturnCommunicator();
        CloseConnection();
        return;

        void RemoveFromCommunicatorList()
        {
            OnCommunicatorDisconnected?.Invoke(communicator);
            if (_communicatorList.ContainsKey(socket)) _communicatorList.Remove(socket);
        }

        void ReturnCommunicator()
        {
            communicator.OnReceivedMessage -= HandleReceivedMessage;
            communicator.OnClose           -= OnCommunicatorClose;
            _communicatorPool.Return(communicator);
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