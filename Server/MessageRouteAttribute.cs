using Protocol;

namespace Server;

[AttributeUsage(AttributeTargets.Method)]
public class MessageRouteAttribute : Attribute
{
    public uint MessageId { get; private set; }

    public MessageRouteAttribute(uint messageId)
    {
        MessageId = messageId;
    }
}