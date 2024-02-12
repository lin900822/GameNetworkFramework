using System.Net;
using System.Net.Sockets;
using Core.Log;

namespace Core.Network;

public class NetworkConnector
{
    public Action<NetworkCommunicator> OnConnected;

    public Action<ReceivedMessageInfo> OnReceivedMessage;

    public Action<Socket> OnClosed;
    
    private Socket            _connectFd;

    public ConnectState ConnectState { get; private set; }

    private NetworkCommunicator _communicator;
    
    public NetworkConnector()
    {
        _communicator = new NetworkCommunicator(new ByteBufferPool(), NetworkConfig.BufferSize);
        ConnectState = ConnectState.None;
    }

    public async void Connect(string ip, int port)
    {
        ConnectState = ConnectState.Connecting;
        var ipAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        try
        {
            _connectFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Log.Log.Info($"Start Connecting to {ip}...");

            await _connectFd.ConnectAsync(ipEndPoint);

            if (!_connectFd.Connected)
            {
                return;
            }

            ConnectState = ConnectState.Connected;
            Log.Log.Info("Connected!");

            _communicator.OnReceivedMessage += OnReceivedMessage;
            _communicator.OnReceivedNothing += OnSessionReceivedNothing;
            
            _communicator.SetActive(_connectFd);
            _communicator.ReceiveAsync();
        }
        catch (Exception e)
        {
            Log.Log.Error(e.ToString());
            ConnectState = ConnectState.Disconnected;
        }
    }

    public void Send(uint messageId, byte[] message, bool isRequest = false, ushort requestId = 0)
    {
        if (_communicator == null) return;
        
        _communicator.Send(messageId, message, isRequest, requestId);
    }
    
    private void OnSessionReceivedNothing(NetworkCommunicator session)
    {
        _communicator.OnReceivedMessage -= OnReceivedMessage;
        _communicator.OnReceivedNothing -= OnSessionReceivedNothing;
        
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
            Log.Log.Error(e.ToString());
        }
        
        _connectFd.Close();
        Log.Log.Info("Connection Closed!");
    }
}