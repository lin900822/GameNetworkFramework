using Dapper;
using Server.Database;
using Server.Repositories;
using SnakeMainServer.PO;

namespace SnakeMainServer.Repositories;

public class PlayerRepository : BaseRepository<PlayerPO>
{
    public PlayerRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    protected override async Task OnInit()
    {
        var sql = @"
                CREATE TABLE IF NOT EXISTS PlayerPO (
                    PlayerId INT UNSIGNED PRIMARY KEY
                );";
        await ExecuteAsync(sql);
        
        if (!await IsColumnExistsAsync("PlayerPO", "Username"))
        {
            await ExecuteAsync("ALTER TABLE PlayerPO ADD Username VARCHAR(50);");
        }
        
        if (!await IsColumnExistsAsync("PlayerPO", "Password"))
        {
            await ExecuteAsync("ALTER TABLE PlayerPO ADD Password VARCHAR(50);");
        }
        
        if (!await IsColumnExistsAsync("PlayerPO", "Coins"))
        {
            await ExecuteAsync("ALTER TABLE PlayerPO ADD Coins INT;");
        }
    }
    
    public async Task<int> Insert(PlayerPO userPo)
    {
        var sql =
            @"
        INSERT INTO PlayerPO(PlayerId,Username,Password,COINS) 
        VALUES(@PlayerId,@Username,@Password,@Coins);
        SELECT @@IDENTITY;
        ";

        return await Insert(userPo, sql);
    }

    public async Task<int> Update(PlayerPO userPo)
    {
        var sql =
            @"
        UPDATE PlayerPO SET
        Username=@Username,
        Password=@Password,
        Coins=@Coins,
        WHERE PlayerId=@PlayerId;
        ";

        return await Update(userPo, sql);
    }

    public async Task<int> Delete(int playerId)
    {
        var sql =
            @"
        DELETE FROM PlayerPO WHERE PlayerId=@PlayerId;
        ";

        return await Delete(playerId, sql);
    }

    public async Task<PlayerPO> SelectOne(int playerId)
    {
        var sql =
            @"
        SELECT * FROM PlayerPO WHERE PlayerId=@PlayerId;
        ";

        return await SelectOne(playerId, sql);
    }

    public async Task<List<PlayerPO>> SelectAll()
    {
        var sql =
            @"
        SELECT * FROM PlayerPO;
        ";

        return await SelectAll(sql);
    }

    public async Task<bool> IsUserExist(string username)
    {
        var sql =
            @"
        SELECT COUNT(*) FROM PlayerPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        var count = await dbConnection.QueryFirstAsync<int>(sql, new { Username = username });
        return count >= 1;
    }

    public async Task<PlayerPO> GetPlayerAsync(string username)
    {
        var sql =
            @"
        SELECT * FROM PlayerPO WHERE username=@Username;
        ";

        using var dbConnection = _dbContext.Connection;
        PlayerPO user = null;
        try
        {
            user = await dbConnection.QuerySingleAsync<PlayerPO>(sql, new { Username = username });
        }
        catch (Exception e)
        {
            // ignored
        }

        return user ?? null;
    }

    public async Task<uint> GetMaxPlayerId()
    {
        var sql =
            @"
        SELECT MAX(PlayerId) FROM PlayerPO;
        ";

        using var dbConnection = _dbContext.Connection;
        uint maxPlayerId = 0;
        try
        {
            maxPlayerId = await dbConnection.QuerySingleAsync<uint>(sql);
        }
        catch (Exception e)
        {
            // ignored
        }

        return maxPlayerId;
    }
}