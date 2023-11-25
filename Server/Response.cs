namespace Server;

public struct Response
{
    public byte[] Message;
    public uint   StateCode;

    public Response(byte[] message, uint stateCode = 0)
    {
        Message   = message;
        StateCode = stateCode;
    }
}