namespace Shared;

public enum MessageId : ushort
{
    HeartBeat = 0,
    
    // 測試
    Echo      = 10,
    EchoAsync = 11,
    Move      = 12,
    Hello     = 13,
    RawByte   = 14,
    Broadcast = 15,
    
    // 對Client
    Register = 101,
    Login = 102,
    
    ClientMax = 32767,
    
    // Server內部
    ServerInfo = 32768,
}