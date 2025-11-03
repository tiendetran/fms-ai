using FAS.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;

namespace FAS.Api.Data;

//public interface IDatabaseContext
//{
//    IDbConnection CreatePostgreSqlConnection();
//    IDbConnection CreateSqlServerConnection();
//}

public class DatabaseContext : IDatabaseContext
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseContext> _logger;

    public DatabaseContext(IConfiguration configuration, ILogger<DatabaseContext> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IDbConnection CreatePostgreSqlConnection()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("PostgreSQL");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("PostgreSQL connection string is not configured");
            }

            var connection = new NpgsqlConnection(connectionString);
            _logger.LogDebug("PostgreSQL connection created");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PostgreSQL connection");
            throw;
        }
    }

    public IDbConnection CreateSqlServerConnection()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("SqlServer");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL Server connection string is not configured");
            }

            var connection = new SqlConnection(connectionString);
            _logger.LogDebug("SQL Server connection created");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SQL Server connection");
            throw;
        }
    }
}
