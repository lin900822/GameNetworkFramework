namespace Server;

[AttributeUsage(AttributeTargets.Method)]
public class MessageRouteAttribute : Attribute
{
    public ushort MessageId { get; private set; }

    public MessageRouteAttribute(ushort messageId)
    {
        MessageId = messageId;
    }
}