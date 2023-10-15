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
            var data = Encoding.Unicode.GetBytes(msg);
            Send(c.Value, 0, data);
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

    private void Send(TCPClient client, UInt16 messageId, byte[] message)
    {
        if (client == null || client.Socket == null || !client.Socket.Connected)
        {
            Logger.Error("Send Failed, client is null or not connected");
            return;
        }
        
        // 打包資料
        var byteBuffer = new ByteBuffer(2 + 2 + message.Length);
        MessageUtils.SetMessage(byteBuffer, messageId, message);
        
        // 透過 SendQueue處理發送不完整問題
        int count = 0;
        lock (client.SendQueue)
        {
            client.SendQueue.Enqueue(byteBuffer);
            count = client.SendQueue.Count;
        }

        // 當 SendQueue只有 1個時發送
        // SendQueue.Count > 1時, 在 OnSend()裡面會持續發送, 直到發送完
        if (count == 1)
        {
            // 準備發送用的 SocketAsyncEventArgs
            var args = _eventArgsPool.Get();
            args.Completed += OnSend;
            args.SetBuffer(args.Offset, byteBuffer.Length);
            args.AcceptSocket = client.Socket;
            
            Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, byteBuffer.Length);
            SendAsync(args);
        }
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
        if (!_clients.TryGetValue(args.AcceptSocket, out var client))
        {
            Logger.Error("Send Failed, client is null");
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
        if (byteBuffer.Length == 0)
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
        else
        {
            // 發送完，歸還args
            args.Completed -= OnSend;
            _eventArgsPool.Return(args);
        }
    }
}