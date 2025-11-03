namespace FAS.Core.Entities;

/// <summary>
/// Lưu trữ tài liệu và embedding vector cho RAG
/// </summary>
public class DocumentEmbedding : BaseEntity
{
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // PDF, Database, Text
    public string Content { get; set; } = string.Empty;
    public string? SourceTable { get; set; } // Tên bảng nếu là database
    public int? SourceRecordId { get; set; } // ID record nếu là database
    public float[]? Embedding { get; set; } // Vector embedding từ Ollama
    public string? Metadata { get; set; } // JSON metadata
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Chat history
/// </summary>
public class ChatHistoryModel : BaseEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string? Context { get; set; } // JSON của các documents được retrieve
    public int TokensUsed { get; set; }
    public DateTime ChatTime { get; set; } = DateTime.UtcNow;
}