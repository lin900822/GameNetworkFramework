namespace Core.Network;

public class NetworkSession : NetworkCommunicator
{
    public object SessionObject { get; set; }
    
    public NetworkSession(ByteBufferPool pool, int bufferSize) : base(pool, bufferSize)
    {
    }

    public override void Release()
    {
        base.Release();
        SessionObject = null;
    }
}