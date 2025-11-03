using System.Data;

namespace FAS.Core.Interfaces
{
    public interface IDbContext
    {
        IDbConnection CreatePostgreSqlConnection();
        IDbConnection CreateSqlServerConnection();
    }
}
