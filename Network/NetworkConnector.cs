using System.Net;
using System.Net.Sockets;
using Log;

namespace Network;

public class NetworkConnector
{
    public Action<NetworkSession> OnConnected;

    public Action<ReceivedMessageInfo> OnReceivedMessage;

    public Action<Socket> OnClosed;
    
    public bool IsConnected => _connectFd.Connected;
    
    private Socket            _connectFd;

    private NetworkSession _session;
    
    public NetworkConnector()
    {
        _session = new NetworkSession(new ByteBufferPool(), NetworkConfig.BufferSize);
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

            _session.OnReceivedMessage += OnReceivedMessage;
            _session.OnReceivedNothing += OnSessionReceivedNothing;
            
            _session.SetActive(_connectFd);
            _session.ReceiveAsync();
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    public void Send(uint messageId, byte[] message, uint stateCode = 0)
    {
        if (_session == null) return;
        
        _session.Send(messageId, message, stateCode);
    }
    
    private void OnSessionReceivedNothing(NetworkSession session)
    {
        _session.OnReceivedMessage -= OnReceivedMessage;
        _session.OnReceivedNothing -= OnSessionReceivedNothing;
        
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