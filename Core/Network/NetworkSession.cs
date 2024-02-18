namespace Core.Network;

public class NetworkSession : NetworkCommunicator
{
    public object SessionObject { get; set; }
    
    public NetworkSession(int bufferSize) : base(bufferSize)
    {
    }

    public override void Release()
    {
        base.Release();
        SessionObject = null;
    }
}