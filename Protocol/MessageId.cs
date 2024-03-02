namespace Protocol;

public enum MessageId : uint
{
    HeartBeat = 0,
    
    Hello = 101,
    Move = 102,
    Register = 103,
    RawByte = 104,
    Broadcast = 105,
    Login = 106,
}