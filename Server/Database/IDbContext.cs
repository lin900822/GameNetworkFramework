using System.Data;

namespace Server.Database;

public interface IDbContext
{
    public IDbConnection Connection { get; }
}