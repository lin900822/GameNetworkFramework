namespace Server;

[AttributeUsage(AttributeTargets.Method)]
public class RouteAttribute : Attribute
{
    public ushort MessageId { get; private set; }

    public RouteAttribute(ushort messageId)
    {
        MessageId = messageId;
    }
}