namespace Server.Repositories;

public interface IBaseRepository<T>
{
    Task<int> Insert(T entity, string insertSql);

    Task<int> Update(T entity, string updateSql);

    Task<int> Delete(int id, string deleteSql);
    
    Task<T> SelectOne(int id, string selectOneSql);

    Task<List<T>> SelectAll(string selectSql);
}