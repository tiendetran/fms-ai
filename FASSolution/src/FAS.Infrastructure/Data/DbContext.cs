using FAS.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;

namespace FAS.Infrastructure.Data
{
    public class DbContext : IDbContext
    {
        private readonly string _postgreSqlConnection;
        private readonly string _sqlServerConnection;

        public DbContext(string postgreSqlConnection, string sqlServerConnection)
        {
            _postgreSqlConnection = postgreSqlConnection;
            _sqlServerConnection = sqlServerConnection;
        }

        public IDbConnection CreatePostgreSqlConnection()
        {
            return new NpgsqlConnection(_postgreSqlConnection);
        }

        public IDbConnection CreateSqlServerConnection()
        {
            return new SqlConnection(_sqlServerConnection);
        }
    }
}
