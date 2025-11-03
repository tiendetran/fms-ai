using Dapper;
using FAS.Core.Entities;
using FAS.Core.Interfaces;

namespace FAS.Infrastructure.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly IDbContext _dbContext;
    protected readonly string _tableName;

    public BaseRepository(IDbContext dbContext, string tableName)
    {
        _dbContext = dbContext;
        _tableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = $"SELECT * FROM {_tableName} WHERE id = @Id AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = $"SELECT * FROM {_tableName} WHERE is_deleted = FALSE ORDER BY id DESC";
        return await connection.QueryAsync<T>(sql);
    }

    public virtual async Task<int> AddAsync(T entity)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        entity.CreatedAt = DateTime.UtcNow;

        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.CanWrite)
            .Select(p => p.Name);

        var columns = string.Join(", ", properties.Select(p => ToSnakeCase(p)));
        var values = string.Join(", ", properties.Select(p => $"@{p}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}) RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        entity.UpdatedAt = DateTime.UtcNow;

        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id" && p.Name != "CreatedAt" && p.CanWrite)
            .Select(p => $"{ToSnakeCase(p.Name)} = @{p.Name}");

        var sql = $"UPDATE {_tableName} SET {string.Join(", ", properties)} WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, entity);
        return result > 0;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = $"UPDATE {_tableName} SET is_deleted = TRUE, updated_at = @UpdatedAt WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow });
        return result > 0;
    }

    protected string ToSnakeCase(string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}
