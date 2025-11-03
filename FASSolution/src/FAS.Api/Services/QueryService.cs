using FAS.Core.Interfaces;

namespace FAS.Api.Services;

public interface IQueryService
{
    Task<QueryResult> QueryAsync(QueryRequest request);
    Task<List<string>> GetSuggestionsAsync(string partialQuery);
}

public class QueryService : IQueryService
{
    private readonly IFMSAgentService _agentService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        IFMSAgentService agentService,
        IVectorStoreService vectorStoreService,
        IDatabaseContext dbContext,
        ILogger<QueryService> logger)
    {
        _agentService = agentService;
        _vectorStoreService = vectorStoreService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<QueryResult> QueryAsync(QueryRequest request)
    {
        try
        {
            _logger.LogInformation("Processing query: {Query}", request.Query);
            var startTime = DateTime.UtcNow;

            // Sử dụng Agent để xử lý query
            var answer = await _agentService.ProcessQueryAsync(request.Query, request.UserId);

            // Lấy các tài liệu liên quan
            var relatedDocs = await _vectorStoreService.SearchSimilarAsync(request.Query, topK: 3);

            // Log query history
            await LogQueryHistoryAsync(request, answer);

            var processingTime = DateTime.UtcNow - startTime;

            return new QueryResult
            {
                Query = request.Query,
                Answer = answer,
                RelatedDocuments = relatedDocs.Select(d => new RelatedDocument
                {
                    Source = d.Source,
                    Content = d.Content.Length > 200
                        ? d.Content.Substring(0, 200) + "..."
                        : d.Content,
                    Similarity = d.Similarity
                }).ToList(),
                ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Query}", request.Query);
            throw;
        }
    }

    public async Task<List<string>> GetSuggestionsAsync(string partialQuery)
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = @"
                SELECT DISTINCT query
                FROM query_history
                WHERE query ILIKE @Pattern
                ORDER BY created_at DESC
                LIMIT 5";

            var suggestions = await connection.QueryAsync<string>(
                sql,
                new { Pattern = $"%{partialQuery}%" });

            return suggestions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return new List<string>();
        }
    }

    private async Task LogQueryHistoryAsync(QueryRequest request, string answer)
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS query_history (
                    id SERIAL PRIMARY KEY,
                    user_id VARCHAR(100),
                    query TEXT NOT NULL,
                    answer TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            ");

            await connection.ExecuteAsync(@"
                INSERT INTO query_history (user_id, query, answer)
                VALUES (@UserId, @Query, @Answer)
            ", new
            {
                UserId = request.UserId,
                Query = request.Query,
                Answer = answer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging query history");
            // Don't throw - logging failure shouldn't break the query
        }
    }
}

public class QueryRequest
{
    public string Query { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
}

public class QueryResult
{
    public string Query { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<RelatedDocument> RelatedDocuments { get; set; } = new();
    public int ProcessingTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RelatedDocument
{
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Similarity { get; set; }
}
