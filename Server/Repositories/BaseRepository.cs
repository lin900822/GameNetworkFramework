using Dapper;
using Server.Database;

namespace Server.Repositories;

public abstract class BaseRepository<T>
{
    protected IDbContext _dbContext { get; }

    protected BaseRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    protected async Task<int> Insert(T entity, string insertSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.QueryFirstOrDefaultAsync<int>(insertSql, entity);
    }

    protected async Task<int> Update(T entity, string updateSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.ExecuteAsync(updateSql, entity);
    }

    protected async Task<int> Delete(int id, string deleteSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.ExecuteAsync(deleteSql, new { Id = id });
    }

    protected async Task<T> SelectOne(int id, string selectOneSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.QueryFirstOrDefaultAsync<T>(selectOneSql, new { Id = id });
    }

    protected async Task<List<T>> SelectAll(string selectSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await Task.Run(() => dbConnection.Query<T>(selectSql).ToList());
    }
}