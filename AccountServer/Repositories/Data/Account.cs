using System.ComponentModel.DataAnnotations;

namespace AccountServer.Repositories.Data;

public class Account
{
    [Key]
    public uint     Id        { get; set; }
    [VarcharLength(50)]
    public string   Username  { get; set; }
    [VarcharLength(50)]
    public string   Password  { get; set; }
    public DateTime CreatedAt { get; set; }
}