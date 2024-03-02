using System.ComponentModel.DataAnnotations;
using Server.Repositories;

namespace ServerDemo.PO;

public class UserPO
{
    [Key]
    public uint Id { get; set; }
    [VarcharLength(50)]
    public string Username { get; set; }
    [VarcharLength(50)]
    public string Password { get; set; }
}