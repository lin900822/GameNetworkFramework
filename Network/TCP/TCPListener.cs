using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPListener : TCPBase
{
    // Variables
    private Socket _listenFd;

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
        Logger.Info($"A Client {clientFd.RemoteEndPoint.ToString()} Connected!");

        // 加入Clients列表
        var client = new TCPClient();
        client.Socket = clientFd;
        _clients.Add(clientFd, client);

        // 開始接收clientFd傳來的訊息
        var args = _eventArgsPool.Get();
        args.Completed += OnReceive;

        args.AcceptSocket = clientFd;
        ReceiveAsync(args, OnReceive);

        // 重置acceptEventArg，並繼續監聽
        acceptEventArg.AcceptSocket = null;
        AcceptAsync(acceptEventArg);
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

        ReceiveAsync(args, OnReceive);
    }

    private void ParseReceivedData(TCPClient client)
    {
        var readBuffer = client.ReadBuffer;
        
        if (!TryParseMessage(readBuffer, out var messageId, out var message))
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
        
        // 準備發送用的 SocketAsyncEventArgs
        var args = _eventArgsPool.Get();
        args.Completed += OnSend;
        args.AcceptSocket = client.Socket;

        if (!InnerSend(messageId, message, client.SendQueue, args, OnSend))
        {
            args.Completed -= OnSend;
            _eventArgsPool.Return(args);
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
            SendAsync(args, OnSend);
        }
        else
        {
            // 發送完，歸還args
            args.Completed -= OnSend;
            _eventArgsPool.Return(args);
        }
    }
}