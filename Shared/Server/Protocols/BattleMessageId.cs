namespace Shared.Server;

public enum BattleMessageId : ushort
{
    M2B_HandShake = 1,
    M2B_CreateRoom = 2,
    
    B2M_RoomCreated = 32768,
}