using System.Data;
using MySql.Data.MySqlClient;

namespace Server.Database;

public class MySqlDbContext : IDbContext
{
    public IDbConnection Connection { get; }

    private string _connectString;
    
    public MySqlDbContext(string connectString)
    {
        _connectString = connectString;
        Connection     = new MySqlConnection(_connectString);
    }
}