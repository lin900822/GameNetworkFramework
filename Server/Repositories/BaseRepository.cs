using System.Data;
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

    public async Task Init()
    {
        await OnInit();
    }

    protected abstract Task OnInit();
    
    protected async Task<int> Insert(T entity, string insertSql)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.QueryFirstOrDefaultAsync<int>(insertSql, entity);
    }

    protected async Task<int> Update(T entity, string updateSql)
    {
        return await ExecuteAsync(updateSql, entity);
    }

    protected async Task<int> Delete(int id, string deleteSql)
    {
        return await ExecuteAsync(deleteSql, new { Id = id });
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

    protected async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();
        return await dbConnection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
    }
    
    protected async Task<bool> IsColumnExistsAsync(string tableName, string columnName)
    {
        string checkColumnExistsQuery = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE table_schema = DATABASE() AND table_name = @TableName AND column_name = @ColumnName;
        ";
        
        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();

        var count = await dbConnection.ExecuteScalarAsync<int>(checkColumnExistsQuery, new { TableName = tableName, ColumnName = columnName });
        return count > 0;
    }
}