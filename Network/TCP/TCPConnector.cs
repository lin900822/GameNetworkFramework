using System.Net;
using System.Net.Sockets;
using System.Text;
using Log;

namespace Network.TCP;

public class TCPConnector
{
    private byte[] _receiveBuffer = new byte[1024];
    
    private Socket _connectFd;

    private ILog _logger = new ConsoleLog();
    
    public void Connect(string ip, int port)
    {
        IPAddress ipAddress = IPAddress.Parse(ip);
        try
        {
            _connectFd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _connectFd.Connect(ipAddress, port); 
            
            _logger.LogInfo($"Start Connecting to {ip}...");
            if (_connectFd.Connected)
            {
                _logger.LogInfo("Connected!");
                
                var receiveThread = new Thread(Receive);
                receiveThread.Start();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
        }
    }

    private void Receive()
    {
        while (true)
        {
            var count = _connectFd.Receive(_receiveBuffer, 0, 1024, SocketFlags.None);

            if (count == 0)
            {
                break;
            }
            
            var msg = Encoding.Unicode.GetString(_receiveBuffer, 0, count);
            Logger.LogInfo(msg);
            Logger.LogInfo(count.ToString());
        }
    }
    
    public void Send(string message)
    {
        byte[] sendBuff = Encoding.Unicode.GetBytes(message);
        _connectFd.Send(sendBuff, sendBuff.Length, SocketFlags.None);
    }

    public void Close()
    {
        _connectFd.Shutdown(SocketShutdown.Both);
        _connectFd.Close();
    }
}