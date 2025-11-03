using System.Data;

namespace FAS.Core.Interfaces;

public interface IDatabaseContext
{
    IDbConnection CreatePostgreSqlConnection();
    IDbConnection CreateSqlServerConnection();
}
