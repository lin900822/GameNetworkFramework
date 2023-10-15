
using System.Text;
using Network.TCP;

TCPConnector connector = new TCPConnector();
connector.Connect("127.0.0.1", 10001);

while (true)
{
    var message = Console.ReadLine();
    if (message.Equals("0"))
    {
        break;
    }

    var data = Encoding.Unicode.GetBytes(message);
    connector.Send(0, data);
}