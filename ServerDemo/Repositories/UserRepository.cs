using Core.Logger;
using Dapper;
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
        INSERT INTO UserPO(Id,Username,Password) 
        VALUES(@Id,@Username,@Password);
        SELECT @@IDENTITY;
        ";

        return await Insert(userPo, sql);
    }

    public async Task<int> Update(UserPO userPo)
    {
        var sql =
            @"
        UPDATE UserPO SET
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
        DELETE FROM UserPO WHERE Id=@Id;
        ";

        return await Delete(id, sql);
    }

    public async Task<UserPO> SelectOne(int id)
    {
        var sql =
            @"
        SELECT * FROM UserPO WHERE Id=@Id;
        ";

        return await SelectOne(id, sql);
    }

    public async Task<List<UserPO>> SelectAll()
    {
        var sql =
            @"
        SELECT * FROM UserPO;
        ";

        return await SelectAll(sql);
    }

    public async Task<bool> IsUserExist(string username)
    {
        var sql =
            @"
        SELECT COUNT(*) FROM UserPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        Log.Debug($"{Environment.CurrentManagedThreadId}: Before QueryFirstAsync");
        var count = await dbConnection.QueryFirstAsync<int>(sql, new { Username = username });
        Log.Debug($"{Environment.CurrentManagedThreadId}: After QueryFirstAsync");
        return count >= 1;
    }

    public async Task<UserPO> GetUserAsync(string username)
    {
        var sql =
        @"
        SELECT * FROM UserPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        var user = await dbConnection.QuerySingleAsync<UserPO>(sql, new { Username = username });
        return user ?? null;
    }

    public async Task<uint> GetMaxId()
    {
        var sql =
            @"
        SELECT MAX(Id) FROM UserPO;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        var maxId = await dbConnection.QuerySingleAsync<uint>(sql);
        return maxId;
    }
}