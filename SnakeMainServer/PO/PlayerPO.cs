namespace SnakeMainServer.PO;

public class PlayerPO
{
    public uint PlayerId { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoggedIn { get; set; }
    public uint Coins { get; set; }
}