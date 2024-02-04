using System.Net;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkConnector
{
    public Action<NetworkCommunicator> OnConnected;

    public Action<ReceivedMessageInfo> OnReceivedMessage;

    public Action<Socket> OnClosed;
    
    public bool IsConnected => _connectFd.Connected;
    
    private Socket            _connectFd;

    private NetworkCommunicator _communicator;
    
    public NetworkConnector()
    {
        _communicator = new NetworkCommunicator(new ByteBufferPool(), NetworkConfig.BufferSize);
    }

    public async void Connect(string ip, int port)
    {
        var ipAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        try
        {
            _connectFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Logger.Info($"Start Connecting to {ip}...");

            await _connectFd.ConnectAsync(ipEndPoint);

            if (!_connectFd.Connected)
            {
                return;
            }
            
            Logger.Info("Connected!");

            _communicator.OnReceivedMessage += OnReceivedMessage;
            _communicator.OnReceivedNothing += OnSessionReceivedNothing;
            
            _communicator.SetActive(_connectFd);
            _communicator.ReceiveAsync();
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    public void Send(uint messageId, byte[] message, uint stateCode = 0)
    {
        if (_communicator == null) return;
        
        _communicator.Send(messageId, message, stateCode);
    }
    
    private void OnSessionReceivedNothing(NetworkCommunicator session)
    {
        _communicator.OnReceivedMessage -= OnReceivedMessage;
        _communicator.OnReceivedNothing -= OnSessionReceivedNothing;
        
        Close();
    } 

    public void Close()
    {
        OnClosed?.Invoke(_connectFd);
        
        try
        {
            _connectFd.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
        
        _connectFd.Close();
        Logger.Info("Connection Closed!");
    }
}