using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Dapper;
using Server.Database;
using Shared.Logger;

[AttributeUsage(AttributeTargets.Property)]
public class VarcharLengthAttribute : Attribute
{
    public int Length { get; }

    public VarcharLengthAttribute(int length)
    {
        Length = length;
    }
}

public class MigrateTool
{
    private readonly IDbContext _dbContext;

    public MigrateTool(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Migrate(Type entityType)
    {
        string tableName = entityType.Name;

        using var dbConnection = _dbContext.Connection;
        dbConnection.Open();

        // 檢查表格是否存在
        bool tableExists = dbConnection.QuerySingleOrDefault<int>(
            $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}' AND TABLE_SCHEMA = '{dbConnection.Database}';"
        ) > 0;

        if (!tableExists)
        {
            // 如果表格不存在，創建表格
            CreateTable(dbConnection, entityType);
        }
        else
        {
            // 如果表格存在，檢查並更新欄位
            UpdateTable(dbConnection, entityType);
        }
    }

    private void CreateTable(IDbConnection connection, Type entityType)
    {
        string tableName = entityType.Name;

        // 假設 InnoDB 引擎，你可以根據需求更改
        string createTableSql = $"CREATE TABLE {tableName} ({GetColumnsDefinition(entityType)}) ENGINE=InnoDB";

        connection.Execute(createTableSql);

        Log.Info($"Table {tableName} created successfully.");
    }

    private void UpdateTable(IDbConnection connection, Type entityType)
    {
        string tableName = entityType.Name;

        // 獲取表格中的現有欄位及其類型
        var existingColumns = GetExistingColumns(connection, tableName);

        // 獲取 C# 類型的所有屬性及其類型
        var properties = GetProperties(entityType);

        // 比較現有欄位和 C# 類型的屬性
        foreach (var existingColumn in existingColumns)
        {
            if (!properties.TryGetValue(existingColumn.Key, out var expectedInfo)) continue;
            
            // 如果欄位類型不匹配，進行修改
            if (!string.Equals(existingColumn.Value.DataType, expectedInfo.Item1, StringComparison.OrdinalIgnoreCase))
            {
                string alterTableSql = $"ALTER TABLE {tableName} MODIFY COLUMN {existingColumn.Key} {expectedInfo.Item1}";

                // 執行 ALTER TABLE 語句
                connection.Execute(alterTableSql);
                Log.Info($"Column {existingColumn.Key} in {tableName} modified successfully.");
            }

            RemovePrimaryKey(connection, tableName, existingColumn, expectedInfo);
        }
        
        foreach (var existingColumn in existingColumns)
        {
            if (!properties.TryGetValue(existingColumn.Key, out var expectedInfo)) continue;
            
            // 檢查是否存在新的主鍵
            AddPrimaryKey(connection, tableName, existingColumn, expectedInfo);
        }

        // 處理需要添加的新欄位
        var newColumns = properties.Keys.Except(existingColumns.Keys);
        AddNewColumns(connection, tableName, newColumns, properties);

        // 處理新的主鍵
        AddNewPrimaryKeys(connection, tableName, existingColumns, properties);
    }

    private Dictionary<string, (string DataType, bool IsPrimaryKey)> GetExistingColumns(IDbConnection connection, string tableName)
    {
        return connection
            .Query<(string ColumnName, string DataType, bool IsPrimaryKey)>(
                $"SELECT COLUMN_NAME, DATA_TYPE, COLUMN_KEY = 'PRI' FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'")
            .ToDictionary(column => column.ColumnName, column => (column.DataType, column.IsPrimaryKey));
    }

    private Dictionary<string, (string DataType, bool IsPrimaryKey)> GetProperties(Type entityType)
    {
        return entityType.GetProperties().ToDictionary(property => property.Name,
            property => (GetColumnType(property), property.GetCustomAttribute<KeyAttribute>() != null));
    }

    private void RemovePrimaryKey(IDbConnection connection, string tableName, KeyValuePair<string, (string DataType, bool IsPrimaryKey)> existingColumn, (string DataType, bool IsPrimaryKey) expectedInfo)
    {
        // 如果原來是主鍵但現在不是，刪除主鍵
        if (existingColumn.Value.IsPrimaryKey && !expectedInfo.IsPrimaryKey)
        {
            string dropPrimaryKeySql = $"ALTER TABLE {tableName} DROP PRIMARY KEY";
            connection.Execute(dropPrimaryKeySql);
            Log.Info($"Primary key removed from column {existingColumn.Key} in {tableName}.");
        }
    }
    
    private void AddPrimaryKey(IDbConnection connection, string tableName, KeyValuePair<string, (string DataType, bool IsPrimaryKey)> existingColumn, (string DataType, bool IsPrimaryKey) expectedInfo)
    {
        // 如果原來不是主鍵但現在是，添加主鍵
        if (!existingColumn.Value.IsPrimaryKey && expectedInfo.IsPrimaryKey)
        {
            string addPrimaryKeySql = $"ALTER TABLE {tableName} ADD PRIMARY KEY ({existingColumn.Key})";
            connection.Execute(addPrimaryKeySql);
            Log.Info($"Primary key added to column {existingColumn.Key} in {tableName}.");
        }
    }

    private void AddNewColumns(IDbConnection connection, string tableName, IEnumerable<string> newColumns, Dictionary<string, (string DataType, bool IsPrimaryKey)> properties)
    {
        foreach (var newColumn in newColumns)
        {
            string addColumnSql = $"ALTER TABLE {tableName} ADD COLUMN {newColumn} {properties[newColumn].DataType}";
            connection.Execute(addColumnSql);
            Log.Info($"Column {newColumn} added to {tableName} successfully.");
        }
    }

    private void AddNewPrimaryKeys(IDbConnection connection, string tableName, Dictionary<string, (string DataType, bool IsPrimaryKey)> existingColumns, Dictionary<string, (string DataType, bool IsPrimaryKey)> properties)
    {
        var newPrimaryKeys = properties.Where(p => p.Value.IsPrimaryKey && !existingColumns.ContainsKey(p.Key)).Select(p => p.Key);
        if (newPrimaryKeys.Any())
        {
            string addPrimaryKeySql = $"ALTER TABLE {tableName} ADD PRIMARY KEY ({string.Join(", ", newPrimaryKeys)})";
            connection.Execute(addPrimaryKeySql);
            Log.Info($"Primary key added to columns {string.Join(", ", newPrimaryKeys)} in {tableName}.");
        }
    }

    private string GetColumnsDefinition(Type entityType)
    {
        var columns = entityType.GetProperties().Select(p => $"{p.Name} {GetColumnType(p)}");
        return string.Join(", ", columns);
    }

    private string GetColumnType(PropertyInfo property)
    {
        // 檢查是否標記為 VarcharLength
        int varcharLength = property.GetCustomAttribute<VarcharLengthAttribute>()?.Length ?? 255;

        switch (Type.GetTypeCode(property.PropertyType))
        {
            case TypeCode.Int16:
                return "SMALLINT";
            case TypeCode.Int32:
                return "INT";
            case TypeCode.Int64:
                return "BIGINT";
            case TypeCode.String:
                return $"VARCHAR({varcharLength})";
            case TypeCode.DateTime:
                return "DATETIME";
            case TypeCode.Boolean:
                return "BOOL";
            case TypeCode.Decimal:
                return "DECIMAL(18,2)";
            case TypeCode.Double:
                return "DOUBLE";
            case TypeCode.Single:
                return "FLOAT";
            case TypeCode.Byte:
                return "TINYINT UNSIGNED";
            case TypeCode.Char:
                return "CHAR(1)";
            case TypeCode.Object:
                // 如果是 Enum 類型，使用 INT
                if (property.PropertyType.IsEnum)
                {
                    return "INT";
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    return "CHAR(36)";
                }
                else if (property.PropertyType == typeof(byte[]))
                {
                    return "BLOB";
                }
                else if (property.PropertyType == typeof(TimeSpan))
                {
                    return "TIME";
                }
                else if (property.PropertyType == typeof(DateTimeOffset))
                {
                    return "DATETIME";
                }
                else if (property.PropertyType == typeof(decimal[]))
                {
                    return "JSON";
                }
                break;
            case TypeCode.UInt16:
                return "SMALLINT UNSIGNED";
            case TypeCode.UInt32:
                return "INT UNSIGNED";
            case TypeCode.UInt64:
                return "BIGINT UNSIGNED";
            case TypeCode.SByte:
                return "TINYINT";
        }

        throw new NotSupportedException($"Unsupported property type: {property.PropertyType.Name}");
    }
}
