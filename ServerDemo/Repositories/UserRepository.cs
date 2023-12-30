using Dapper;
using Log;
using Server;
using Server.Database;
using Server.Repositories;
using ServerDemo.PO;

namespace ServerDemo.Repositories;

public class UserRepository : BaseRepository<UserPO>
{
    public UserRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<int> Insert(UserPO userPo)
    {
        var sql =
        @"
        INSERT INTO User(Id,Username,Password) 
        VALUES(@Id,@Username,@Password);
        SELECT @@IDENTITY;
        ";

        return await Insert(userPo, sql);
    }

    public async Task<int> Update(UserPO userPo)
    {
        var sql =
        @"
        UPDATE User SET
        Username=@Username,
        Password=@Password
        WHERE Id=@Id;
        ";

        return await Update(userPo, sql);
    }

    public async Task<int> Delete(int id)
    {
        var sql = 
        @"
        DELETE FROM User WHERE Id=@Id;
        ";

        return await Delete(id, sql);
    }

    public async Task<UserPO> SelectOne(int id)
    {
        var sql = 
        @"
        SELECT * FROM User WHERE Id=@Id;
        ";

        return await SelectOne(id, sql);
    }

    public async Task<List<UserPO>> SelectAll()
    {
        var sql = 
        @"
        SELECT * FROM User;
        ";

        return await SelectAll(sql);
    }

    public async Task<bool> IsUserExist(string username)
    {
        var sql = 
        @"
        SELECT COUNT(*) FROM User WHERE username=@Username;
        ";
        
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        Logger.Debug($"{Environment.CurrentManagedThreadId}: Before QueryFirstAsync");
        var count = await dbConnection.QueryFirstAsync<int>(sql, new { Username = username });
        Logger.Debug($"{Environment.CurrentManagedThreadId}: After QueryFirstAsync");
        return count >= 1;
    }
}