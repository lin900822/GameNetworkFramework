using Dapper;
using LittleOnlineGameServer.Repositories.Data;
using Server.Database;
using Server.Repositories;
using Shared.Logger;

namespace LittleOnlineGameServer.Repositories;

public class AccountRepository : BaseRepository<AccountPO>
{
    public AccountRepository(IDbContext dbContext) : base(dbContext)
    {
    }
    
    protected override async Task OnInit()
    {
        var sql = @"
                CREATE TABLE IF NOT EXISTS AccountPO (
                    Id INT UNSIGNED PRIMARY KEY
                );";
        await ExecuteAsync(sql);
        
        if (!await IsColumnExistsAsync("AccountPO", "Username"))
        {
            await ExecuteAsync("ALTER TABLE AccountPO ADD Username VARCHAR(50);");
        }
        
        if (!await IsColumnExistsAsync("AccountPO", "Password"))
        {
            await ExecuteAsync("ALTER TABLE AccountPO ADD Password VARCHAR(50);");
        }
        
        if (!await IsColumnExistsAsync("AccountPO", "CreatedAt"))
        {
            await ExecuteAsync("ALTER TABLE AccountPO ADD CreatedAt DATETIME;");
        }
        
        Log.Info($"AccountRepository Init Success!");
    }
    
    public async Task<int> Insert(AccountPO accountPo)
    {
        var sql =
            @"
        INSERT INTO AccountPO(Id,Username,Password) 
        VALUES(@Id,@Username,@Password);
        ";

        return await Insert(accountPo, sql);
    }

    public async Task<int> Update(AccountPO accountPo)
    {
        var sql =
            @"
        UPDATE AccountPO SET
        Username=@Username,
        Password=@Password
        WHERE Id=@Id;
        ";

        return await Update(accountPo, sql);
    }

    public async Task<int> Delete(int id)
    {
        var sql =
            @"
        DELETE FROM AccountPO WHERE Id=@Id;
        ";

        return await Delete(id, sql);
    }

    public async Task<AccountPO> SelectOne(int id)
    {
        var sql =
            @"
        SELECT * FROM AccountPO WHERE Id=@Id;
        ";

        return await SelectOne(id, sql);
    }

    public async Task<List<AccountPO>> SelectAll()
    {
        var sql =
            @"
        SELECT * FROM AccountPO;
        ";

        return await SelectAll(sql);
    }

    public async Task<bool> IsAccountExist(string username)
    {
        var sql =
            @"
        SELECT COUNT(*) FROM AccountPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        var count = await dbConnection.QueryFirstAsync<int>(sql, new { Username = username });
        return count >= 1;
    }

    public async Task<AccountPO> GetAccountAsync(string username)
    {
        var sql =
            @"
        SELECT * FROM AccountPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        try
        {
            var account = await dbConnection.QuerySingleAsync<AccountPO>(sql, new { Username = username });
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
        SELECT MAX(Id) FROM AccountPO;
        ";

        using var dbConnection = _dbContext.Connection;

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