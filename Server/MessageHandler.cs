namespace Server;

[MessageHandler]
public class MessageHandler
{
    protected readonly ServerApp _serverApp;

    public MessageHandler(ServerApp serverApp)
    {
        _serverApp = serverApp;
    }
}