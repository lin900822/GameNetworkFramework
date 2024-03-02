using AccountServer.Repositories.Data;
using Dapper;
using Server.Database;
using Server.Repositories;

namespace AccountServer.Repositories;

public class AccountRepository : BaseRepository<Account>
{
    public AccountRepository(IDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<int> Insert(Account account)
    {
        var sql =
            @"
        INSERT INTO Account(Id,Username,Password) 
        VALUES(@Id,@Username,@Password);
        SELECT @@IDENTITY;
        ";

        return await Insert(account, sql);
    }

    public async Task<int> Update(Account account)
    {
        var sql =
            @"
        UPDATE Account SET
        Username=@Username,
        Password=@Password
        WHERE Id=@Id;
        ";

        return await Update(account, sql);
    }

    public async Task<int> Delete(int id)
    {
        var sql =
            @"
        DELETE FROM Account WHERE Id=@Id;
        ";

        return await Delete(id, sql);
    }

    public async Task<Account> SelectOne(int id)
    {
        var sql =
            @"
        SELECT * FROM Account WHERE Id=@Id;
        ";

        return await SelectOne(id, sql);
    }

    public async Task<List<Account>> SelectAll()
    {
        var sql =
            @"
        SELECT * FROM Account;
        ";

        return await SelectAll(sql);
    }

    public async Task<bool> IsAccountExist(string username)
    {
        var sql =
            @"
        SELECT COUNT(*) FROM Account WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        var count = await dbConnection.QueryFirstAsync<int>(sql, new { Username = username });
        return count >= 1;
    }

    public async Task<Account> GetAccountAsync(string username)
    {
        var sql =
            @"
        SELECT * FROM Account WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        try
        {
            var account = await dbConnection.QuerySingleAsync<Account>(sql, new { Username = username });
            return account ?? null;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<uint> GetMaxId()
    {
        var sql =
            @"
        SELECT MAX(Id) FROM Account;
        ";

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();

        try
        {
            return await dbConnection.QuerySingleAsync<uint>(sql);
        }
        catch (Exception e)
        {
            return 0;
        }
    }
}