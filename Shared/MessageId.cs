namespace Shared;

public enum MessageId : ushort
{
    HeartBeat = 0,
    
    // 測試
    Hello     = 10,
    Move      = 11,
    RawByte   = 12,
    Broadcast = 13,
    
    // 對Client
    Register = 101,
    Login = 102,
    
    ClientMax = 32767,
    
    // Server內部
    ServerInfo = 32768,
}