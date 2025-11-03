using Dapper;
using FAS.Core.Interfaces;
using System.Data;

namespace FAS.Api.Services;

public interface IDatabaseSyncService
{
    Task SyncAllTablesAsync();
    Task SyncTableAsync(string tableName);
    Task<SyncStatus> GetSyncStatusAsync();
}

public class DatabaseSyncService : IDatabaseSyncService
{
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly int _batchSize;

    // Danh sách các bảng chính cần sync
    private readonly string[] _tablesToSync = new[]
    {
        "tbl_GBMaterial",           // Nguyên liệu
        "tbl_Product",              // Thành phẩm
        "tbl_GBVendor",             // Nhà cung cấp
        "tbl_GBXNKManufacture",     // Nhà sản xuất
        "tbl_Customer",             // Khách hàng
        "tbl_Warehouse",            // Kho
        "tbl_GBXNKPO",              // Phiếu nhập nguyên liệu
        "tbl_GBXNKLVC",             // Kiểm tra nguyên liệu (LAB)
        "tbl_SalesOrder",           // Đơn hàng
        "tbl_ProductionPlan",       // Kế hoạch sản xuất
        "tbl_WorkOrder",            // Lệnh sản xuất
        "tbl_SalesDelivery",        // Xuất bán hàng
        "tbl_Inventory"             // Tồn kho
    };

    public DatabaseSyncService(
        IDatabaseContext dbContext,
        IConfiguration configuration,
        ILogger<DatabaseSyncService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _batchSize = configuration.GetValue<int>("SyncSettings:BatchSize", 1000);
    }

    public async Task SyncAllTablesAsync()
    {
        _logger.LogInformation("Starting full database sync");
        var startTime = DateTime.UtcNow;

        try
        {
            foreach (var tableName in _tablesToSync)
            {
                await SyncTableAsync(tableName);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Full database sync completed in {Duration}", duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full database sync");
            throw;
        }
    }

    public async Task SyncTableAsync(string tableName)
    {
        _logger.LogInformation("Syncing table: {TableName}", tableName);

        try
        {
            using var sqlServerConnection = _dbContext.CreateSqlServerConnection();
            using var postgresConnection = _dbContext.CreatePostgreSqlConnection();

            sqlServerConnection.Open();
            postgresConnection.Open();

            // Get table schema
            var schema = await GetTableSchemaAsync(sqlServerConnection, tableName);

            // Create table in PostgreSQL if not exists
            await CreatePostgresTableAsync(postgresConnection, tableName, schema);

            // Get row count
            var countSql = $"SELECT COUNT(*) FROM {tableName}";
            var totalRows = await sqlServerConnection.ExecuteScalarAsync<int>(countSql);

            _logger.LogInformation("Table {TableName} has {TotalRows} rows", tableName, totalRows);

            // Sync data in batches
            var offset = 0;
            while (offset < totalRows)
            {
                var dataSql = $@"
                    SELECT * FROM {tableName}
                    ORDER BY (SELECT NULL)
                    OFFSET {offset} ROWS
                    FETCH NEXT {_batchSize} ROWS ONLY";

                var data = await sqlServerConnection.QueryAsync(dataSql);
                var dataList = data.ToList();

                if (dataList.Any())
                {
                    await BulkInsertToPostgresAsync(postgresConnection, tableName, dataList, schema);
                    _logger.LogDebug("Synced {Count} rows for table {TableName}", dataList.Count, tableName);
                }

                offset += _batchSize;
            }

            // Update sync status
            await UpdateSyncStatusAsync(postgresConnection, tableName, totalRows);

            _logger.LogInformation("Table {TableName} synced successfully", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing table: {TableName}", tableName);
            throw;
        }
    }

    private async Task<List<ColumnSchema>> GetTableSchemaAsync(IDbConnection connection, string tableName)
    {
        var sql = @"
            SELECT 
                COLUMN_NAME as ColumnName,
                DATA_TYPE as DataType,
                CHARACTER_MAXIMUM_LENGTH as MaxLength,
                IS_NULLABLE as IsNullable
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

        var schema = await connection.QueryAsync<ColumnSchema>(sql, new { TableName = tableName });
        return schema.ToList();
    }

    private async Task CreatePostgresTableAsync(
        IDbConnection connection,
        string tableName,
        List<ColumnSchema> schema)
    {
        var columns = schema.Select(c =>
        {
            var pgType = MapSqlServerTypeToPostgres(c.DataType, c.MaxLength);
            var nullable = c.IsNullable == "YES" ? "" : "NOT NULL";
            return $"{c.ColumnName} {pgType} {nullable}";
        });

        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {tableName.ToLower()} (
                {string.Join(",\n                ", columns)}
            );";

        await connection.ExecuteAsync(createTableSql);
    }

    private async Task BulkInsertToPostgresAsync(
        IDbConnection connection,
        string tableName,
        List<dynamic> data,
        List<ColumnSchema> schema)
    {
        if (!data.Any()) return;

        var columnNames = schema.Select(c => c.ColumnName).ToList();
        var columns = string.Join(", ", columnNames);
        var parameters = string.Join(", ", columnNames.Select(c => $"@{c}"));

        var upsertSql = $@"
            INSERT INTO {tableName.ToLower()} ({columns})
            VALUES ({parameters})
            ON CONFLICT DO NOTHING;";

        await connection.ExecuteAsync(upsertSql, data);
    }

    private async Task UpdateSyncStatusAsync(IDbConnection connection, string tableName, int rowCount)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS sync_status (
                table_name VARCHAR(255) PRIMARY KEY,
                last_sync_time TIMESTAMP,
                row_count INTEGER,
                status VARCHAR(50)
            );

            INSERT INTO sync_status (table_name, last_sync_time, row_count, status)
            VALUES (@TableName, @SyncTime, @RowCount, @Status)
            ON CONFLICT (table_name) 
            DO UPDATE SET 
                last_sync_time = @SyncTime,
                row_count = @RowCount,
                status = @Status;";

        await connection.ExecuteAsync(sql, new
        {
            TableName = tableName.ToLower(),
            SyncTime = DateTime.UtcNow,
            RowCount = rowCount,
            Status = "Completed"
        });
    }

    public async Task<SyncStatus> GetSyncStatusAsync()
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = @"
                SELECT 
                    table_name as TableName,
                    last_sync_time as LastSyncTime,
                    row_count as RowCount,
                    status as Status
                FROM sync_status
                ORDER BY last_sync_time DESC";

            var tables = await connection.QueryAsync<TableSyncInfo>(sql);

            return new SyncStatus
            {
                Tables = tables.ToList(),
                LastSyncTime = tables.Any() ? tables.Max(t => t.LastSyncTime) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            throw;
        }
    }

    private string MapSqlServerTypeToPostgres(string sqlServerType, int? maxLength)
    {
        return sqlServerType.ToLower() switch
        {
            "int" => "INTEGER",
            "bigint" => "BIGINT",
            "smallint" => "SMALLINT",
            "tinyint" => "SMALLINT",
            "bit" => "BOOLEAN",
            "decimal" or "numeric" => "NUMERIC",
            "money" or "smallmoney" => "NUMERIC(19,4)",
            "float" => "DOUBLE PRECISION",
            "real" => "REAL",
            "date" => "DATE",
            "datetime" or "datetime2" or "smalldatetime" => "TIMESTAMP",
            "time" => "TIME",
            "char" => maxLength.HasValue ? $"CHAR({maxLength})" : "CHAR(1)",
            "varchar" => maxLength.HasValue && maxLength > 0 ? $"VARCHAR({maxLength})" : "TEXT",
            "nchar" => maxLength.HasValue ? $"CHAR({maxLength})" : "CHAR(1)",
            "nvarchar" => maxLength.HasValue && maxLength > 0 ? $"VARCHAR({maxLength})" : "TEXT",
            "text" or "ntext" => "TEXT",
            "uniqueidentifier" => "UUID",
            "varbinary" or "binary" or "image" => "BYTEA",
            _ => "TEXT"
        };
    }
}

public class ColumnSchema
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public string IsNullable { get; set; } = string.Empty;
}

public class SyncStatus
{
    public List<TableSyncInfo> Tables { get; set; } = new();
    public DateTime? LastSyncTime { get; set; }
}

public class TableSyncInfo
{
    public string TableName { get; set; } = string.Empty;
    public DateTime LastSyncTime { get; set; }
    public int RowCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
