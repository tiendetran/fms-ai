using Dapper;
using FAS.Core.Entities;
using FAS.Core.Interfaces;
using Pgvector;

namespace FAS.Infrastructure.Repositories;

public class DocumentEmbeddingRepository : BaseRepository<DocumentEmbedding>, IDocumentEmbeddingRepository
{
    public DocumentEmbeddingRepository(IDbContext dbContext)
        : base(dbContext, "document_embeddings")
    {
    }

    public async Task<IEnumerable<DocumentEmbedding>> SearchSimilarAsync(float[] queryEmbedding, int topK = 5)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();

        var vector = new Vector(queryEmbedding);
        var sql = @"
            SELECT 
                id, document_name, document_type, content, 
                source_table, source_record_id, metadata, indexed_at,
                1 - (embedding <=> @QueryEmbedding) as similarity
            FROM document_embeddings
            WHERE is_deleted = FALSE
            ORDER BY embedding <=> @QueryEmbedding
            LIMIT @TopK";

        var results = await connection.QueryAsync<dynamic>(sql, new { QueryEmbedding = vector, TopK = topK });

        return results.Select(r => new DocumentEmbedding
        {
            Id = r.id,
            DocumentName = r.document_name,
            DocumentType = r.document_type,
            Content = r.content,
            SourceTable = r.source_table,
            SourceRecordId = r.source_record_id,
            Metadata = r.metadata,
            IndexedAt = r.indexed_at
        });
    }

    public async Task<IEnumerable<DocumentEmbedding>> GetByDocumentTypeAsync(string documentType)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM document_embeddings WHERE document_type = @DocumentType AND is_deleted = FALSE";
        return await connection.QueryAsync<DocumentEmbedding>(sql, new { DocumentType = documentType });
    }

    public async Task<bool> DeleteBySourceAsync(string sourceTable, int sourceRecordId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = @"
            UPDATE document_embeddings 
            SET is_deleted = TRUE, updated_at = @UpdatedAt 
            WHERE source_table = @SourceTable AND source_record_id = @SourceRecordId";

        var result = await connection.ExecuteAsync(sql, new
        {
            SourceTable = sourceTable,
            SourceRecordId = sourceRecordId,
            UpdatedAt = DateTime.UtcNow
        });

        return result > 0;
    }

    public override async Task<int> AddAsync(DocumentEmbedding entity)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        entity.CreatedAt = DateTime.UtcNow;
        entity.IndexedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO document_embeddings 
            (document_name, document_type, content, source_table, source_record_id, 
             embedding, metadata, indexed_at, created_at, created_by, is_deleted)
            VALUES 
            (@DocumentName, @DocumentType, @Content, @SourceTable, @SourceRecordId,
             @Embedding, @Metadata::jsonb, @IndexedAt, @CreatedAt, @CreatedBy, @IsDeleted)
            RETURNING id";

        var vector = entity.Embedding != null ? new Vector(entity.Embedding) : null;

        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            entity.DocumentName,
            entity.DocumentType,
            entity.Content,
            entity.SourceTable,
            entity.SourceRecordId,
            Embedding = vector,
            entity.Metadata,
            entity.IndexedAt,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.IsDeleted
        });
    }
}
