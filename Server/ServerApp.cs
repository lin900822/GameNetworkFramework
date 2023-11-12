using System.Reflection;
using Network;

namespace Server;

public class ServerApp
{
    public MessageRouter   Router          => messageRouter;
    public NetworkListener NetworkListener => networkListener;
    
    private MessageRouter   messageRouter;
    private NetworkListener networkListener;

    public ServerApp(int maxSessionCount = 10)
    {
        messageRouter   = new MessageRouter();
        networkListener = new NetworkListener(maxSessionCount);

        networkListener.OnReceivedMessage += messageRouter.ReceiveMessage;
        RegisterMessageHandlers();
    }

    public void StartListening(int port)
    {
        networkListener.Listen("0.0.0.0", port);
        
        while (true)
        {
            messageRouter.OnUpdateLogic();
        }
    }

    public void Send(NetworkSession session, UInt16 messageId, byte[] message)
    {
        networkListener.Send(session, messageId, message);
    }

    private void RegisterMessageHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();
        
        foreach (var type in types)
        {
            if (!type.IsDefined(typeof(MessageHandlerAttribute), true)) continue;

            var instance = Activator.CreateInstance(type, this);
            
            var methodInfos = type.GetMethods();
            foreach (var methodInfo in methodInfos)
            {
                if(!methodInfo.IsDefined(typeof(MessageRouteAttribute), false)) continue;
                
                var action = (Action<MessagePack>)Delegate.CreateDelegate(typeof(Action<MessagePack>), instance, methodInfo);
                var routeAttribute = methodInfo.GetCustomAttribute<MessageRouteAttribute>();
                messageRouter.RegisterMessageHandler(routeAttribute.MessageId, action);
            }
        }
    }
}