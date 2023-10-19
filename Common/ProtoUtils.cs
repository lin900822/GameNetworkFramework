using Google.Protobuf;
using Log;

namespace Common;

public static class ProtoUtils
{
    public static byte[] Encode(IMessage message)
    {
        return message.ToByteArray();
    }

    public static T Decode<T>(byte[] message) where T : IMessage, new()
    {
        try
        {
            var value = new T();

            value.MergeFrom(message);
            return value;
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }

        return default(T);
    }
}