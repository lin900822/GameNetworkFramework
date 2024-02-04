namespace Network;

public class NetworkSession : NetworkCommunicator
{
    public object SessionObject { get; set; }
    
    public NetworkSession(ByteBufferPool pool, int bufferSize) : base(pool, bufferSize)
    {
    }

    public override void SetInactive()
    {
        base.SetInactive();
        SessionObject = null;
    }
}