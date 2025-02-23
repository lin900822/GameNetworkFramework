namespace SnakeBattleServer;

public class Room
{
    public string KeyToEnterRoome { get; private set; }
    public uint Player1Id { get; private set; }
    public uint Player2Id { get; private set; }
    

    public Room(string keyToEnterRoome, uint player1Id, uint player2Id)
    {
        KeyToEnterRoome = keyToEnterRoome;
        Player1Id = player1Id;
        Player2Id = player2Id;
    }
    
    public void Update()
    {
    }
}