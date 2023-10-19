using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPListener : TCPService
{
    public int ConnectionCount => _clients.Count;

    // Variables
    private Socket _listenFd;

    public  Dictionary<Socket, TCPClient> Clients => _clients;
    private Dictionary<Socket, TCPClient> _clients;

    private SocketAsyncEventArgsPool _eventArgsPool;

    public TCPListener()
    {
        _eventArgsPool = new SocketAsyncEventArgsPool(DefaultPoolCapacity, EventArgsBufferSize);
        _clients       = new Dictionary<Socket, TCPClient>();
    }

    public void Listen(string ip, int port)
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
        var client = new TCPClient();
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

    protected override void OnReceive(object sender, SocketAsyncEventArgs args)
    {
        var client = _clients[args.AcceptSocket];

        var receiveCount = args.BytesTransferred;
        var isNotSuccess = args.SocketError != SocketError.Success;

        if (receiveCount <= 0 || isNotSuccess)
        {
            Close(client);
            return;
        }

        var readBuffer = client.ReadBuffer;

        readBuffer.Write(args.Buffer, args.Offset, receiveCount);

        ParseReceivedData(client);

        ReceiveAsync(args);
    }

    private void ParseReceivedData(TCPClient client)
    {
        var readBuffer = client.ReadBuffer;

        if (!TryParseMessage(readBuffer, out var messageId, out var message))
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

    private void Close(TCPClient client)
    {
        var socket            = client.Socket;
        var socketEndPointStr = socket.RemoteEndPoint?.ToString();

        var receiveArgs = client.ReceiveArgs;
        var sendArgs    = client.SendArgs;

        receiveArgs.Completed -= OnReceive;
        sendArgs.Completed    -= OnSend;

        _eventArgsPool.Return(receiveArgs);
        _eventArgsPool.Return(sendArgs);

        lock (_clients)
        {
            if (_clients.ContainsKey(socket)) _clients.Remove(socket);
        }

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

    public void Send(TCPClient client, UInt16 messageId, byte[] message)
    {
        if (client == null || client.Socket == null || !client.Socket.Connected)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }

        var sendArgs = client.SendArgs;
        InnerSend(messageId, message, client.SendQueue, sendArgs);
    }

    protected override void OnSend(object sender, SocketAsyncEventArgs args)
    {
        if (!_clients.TryGetValue(args.AcceptSocket, out var client))
        {
            Logger.Error("Send Failed, client is null");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            Logger.Error($"Send Failed, Socket Error: {args.SocketError}");
            return;
        }

        var count = args.BytesTransferred;

        ByteBuffer byteBuffer;
        lock (client.SendQueue)
        {
            byteBuffer = client.SendQueue.First();
        }

        byteBuffer.SetReadIndex(byteBuffer.ReadIndex + count);

        // 完整發送完一個ByteBuffer的資料
        if (byteBuffer.Length <= 0)
        {
            lock (client.SendQueue)
            {
                client.SendQueue.Dequeue();
                if (client.SendQueue.Count >= 1)
                {
                    byteBuffer = client.SendQueue.First();
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
            args.SetBuffer(args.Offset, byteBuffer.Length);
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, byteBuffer.Length);
            SendAsync(args);
        }
    }

    public void Debug()
    {
        _eventArgsPool.Debug();
    }
}