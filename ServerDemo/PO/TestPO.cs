using System.ComponentModel.DataAnnotations;
using Server.Repositories;

namespace ServerDemo.PO;

public class TestPO
{
    [Key]               public uint     Id        { get; set; }
    [VarcharLength(50)] public string   Name      { get; set; }
    public                     DateTime CreatedAt { get; set; }
}