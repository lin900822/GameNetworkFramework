using System.Net;
using System.Net.Sockets;
using Core.Logger;

namespace Core.Network;

public class NetworkConnector
{
    public Action<NetworkCommunicator> OnConnected;

    public Action<NetworkCommunicator, ReceivedMessageInfo> OnReceivedMessage;

    public Action<Socket> OnClosed;

    private Socket _connectFd;

    public ConnectState ConnectState { get; private set; }

    private NetworkCommunicator _communicator;

    public NetworkConnector()
    {
        _communicator = new NetworkCommunicator(NetworkConfig.BufferSize);
        ConnectState = ConnectState.None;
    }

    public async Task Connect(string ip, int port)
    {
        ConnectState = ConnectState.Connecting;
        var ipAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        try
        {
            _connectFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Log.Info($"Start Connecting to {ip}:{port}...");

            await _connectFd.ConnectAsync(ipEndPoint);

            if (!_connectFd.Connected)
            {
                ConnectState = ConnectState.Disconnected;
                return;
            }

            ConnectState = ConnectState.Connected;
            Log.Info("Connected!");

            _communicator.OnReceivedMessage += OnReceivedMessage;
            _communicator.OnClose += OnCommunicatorClose;

            _communicator.Init(_connectFd);
            _communicator.ReceiveAsync();
            
            OnConnected?.Invoke(_communicator);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            ConnectState = ConnectState.Disconnected;
        }
    }

    public void Update()
    {
        _communicator.HandleMessages();
    }

    public void Send(ushort messageId, byte[] message, bool isRequest = false, ushort requestId = 0)
    {
        if (_communicator == null) return;

        _communicator.Send(messageId, message, isRequest, requestId);
    }
    
    public void Send(ushort messageId, ByteBuffer message, bool isRequest = false, ushort requestId = 0)
    {
        if (_communicator == null) return;

        _communicator.Send(messageId, message, isRequest, requestId);
    }

    private void OnCommunicatorClose(NetworkCommunicator communicator)
    {
        _communicator.OnReceivedMessage -= OnReceivedMessage;
        _communicator.OnClose -= OnCommunicatorClose;

        ConnectState = ConnectState.Disconnected;

        Close();
    }

    private void Close()
    {
        OnClosed?.Invoke(_connectFd);

        try
        {
            _connectFd.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        _connectFd.Close();
        Log.Info("Connection Closed!");
    }
}