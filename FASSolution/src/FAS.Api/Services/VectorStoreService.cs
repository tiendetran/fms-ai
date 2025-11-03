using Dapper;
using FAS.Core.Interfaces;
using Pgvector;

namespace FAS.Api.Services;

public interface IVectorStoreService
{
    Task InitializeVectorExtensionAsync();
    Task<int> StoreDocumentAsync(string documentId, string content, string source, Dictionary<string, object>? metadata = null);
    Task<List<SearchResult>> SearchSimilarAsync(string query, int topK = 5);
    Task<bool> DeleteDocumentAsync(string documentId);
}

public class VectorStoreService : IVectorStoreService
{
    private readonly IDatabaseContext _dbContext;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<VectorStoreService> _logger;
    private readonly int _vectorDimensions;

    public VectorStoreService(
        IDatabaseContext dbContext,
        IOllamaService ollamaService,
        IConfiguration configuration,
        ILogger<VectorStoreService> logger)
    {
        _dbContext = dbContext;
        _ollamaService = ollamaService;
        _logger = logger;
        _vectorDimensions = configuration.GetValue<int>("VectorSettings:Dimensions", 768);
    }

    public async Task InitializeVectorExtensionAsync()
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            // Enable pgvector extension
            await connection.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector;");

            // Create documents table with vector column
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS document_embeddings (
                    id SERIAL PRIMARY KEY,
                    document_id VARCHAR(255) UNIQUE NOT NULL,
                    content TEXT NOT NULL,
                    source VARCHAR(500),
                    metadata JSONB,
                    embedding vector({_vectorDimensions}),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_document_embeddings_vector 
                ON document_embeddings USING ivfflat (embedding vector_cosine_ops)
                WITH (lists = 100);

                CREATE INDEX IF NOT EXISTS idx_document_embeddings_document_id 
                ON document_embeddings(document_id);

                CREATE INDEX IF NOT EXISTS idx_document_embeddings_source 
                ON document_embeddings(source);
            ";

            await connection.ExecuteAsync(createTableSql);
            _logger.LogInformation("Vector store initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing vector store");
            throw;
        }
    }

    public async Task<int> StoreDocumentAsync(
        string documentId,
        string content,
        string source,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            // Generate embedding
            var embedding = await _ollamaService.GenerateEmbeddingAsync(content);
            var vector = new Vector(embedding);

            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = @"
                INSERT INTO document_embeddings (document_id, content, source, metadata, embedding, updated_at)
                VALUES (@DocumentId, @Content, @Source, @Metadata::jsonb, @Embedding, CURRENT_TIMESTAMP)
                ON CONFLICT (document_id) 
                DO UPDATE SET 
                    content = EXCLUDED.content,
                    embedding = EXCLUDED.embedding,
                    metadata = EXCLUDED.metadata,
                    updated_at = CURRENT_TIMESTAMP
                RETURNING id;
            ";

            var metadataJson = metadata != null
                ? System.Text.Json.JsonSerializer.Serialize(metadata)
                : null;

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                DocumentId = documentId,
                Content = content,
                Source = source,
                Metadata = metadataJson,
                Embedding = vector
            });

            _logger.LogInformation("Document stored with ID: {DocumentId}", documentId);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<SearchResult>> SearchSimilarAsync(string query, int topK = 5)
    {
        try
        {
            // Generate embedding for query
            var queryEmbedding = await _ollamaService.GenerateEmbeddingAsync(query);
            var queryVector = new Vector(queryEmbedding);

            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = @"
                SELECT 
                    document_id,
                    content,
                    source,
                    metadata,
                    1 - (embedding <=> @QueryVector) as similarity
                FROM document_embeddings
                WHERE embedding IS NOT NULL
                ORDER BY embedding <=> @QueryVector
                LIMIT @TopK;
            ";

            var results = await connection.QueryAsync<SearchResult>(sql, new
            {
                QueryVector = queryVector,
                TopK = topK
            });

            _logger.LogInformation("Found {Count} similar documents", results.Count());
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar documents");
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = "DELETE FROM document_embeddings WHERE document_id = @DocumentId";
            var rowsAffected = await connection.ExecuteAsync(sql, new { DocumentId = documentId });

            _logger.LogInformation("Deleted document: {DocumentId}", documentId);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
            throw;
        }
    }
}

public class SearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public float Similarity { get; set; }
}
