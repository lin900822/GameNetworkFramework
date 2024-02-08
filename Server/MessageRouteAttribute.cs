using Protocol;

namespace Server;

[AttributeUsage(AttributeTargets.Method)]
public class MessageRouteAttribute : Attribute
{
    public MessageId MessageId { get; private set; }

    public MessageRouteAttribute(MessageId messageId)
    {
        MessageId = messageId;
    }
}