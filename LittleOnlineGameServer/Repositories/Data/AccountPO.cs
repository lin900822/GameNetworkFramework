using System.ComponentModel.DataAnnotations;

namespace LittleOnlineGameServer.Repositories.Data;

public class AccountPO
{
    [Key]
    public uint     Id        { get; set; }
    
    public string   Username  { get; set; }
    
    public string   Password  { get; set; }
    public DateTime CreatedAt { get; set; }
}