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

    public static Response Create(uint stateCode = 0)
    {
        var response = new Response()
        {
            Message   = Array.Empty<byte>(),
            StateCode = stateCode,
        };
        return response;
    } 

    public static Response Create(byte[] message = null, uint stateCode = 0)
    {
        var response = new Response()
        {
            Message = message == null ? Array.Empty<byte>() : message,
            StateCode = stateCode,
        };
        return response;
    }

    public static Response None
    {
        get
        {
            var response = new Response()
            {
                Message = null,
            };
            return response;
        }
    } 
}