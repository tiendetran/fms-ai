using FAS.Core.DTOs;
using FAS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FAS.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly IOllamaService _ollamaService;
    private readonly IDocumentEmbeddingRepository _documentRepository;
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IOllamaService ollamaService,
        IDocumentEmbeddingRepository documentRepository,
        IChatHistoryRepository chatHistoryRepository,
        ILogger<RagService> logger)
    {
        _ollamaService = ollamaService;
        _documentRepository = documentRepository;
        _chatHistoryRepository = chatHistoryRepository;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        try
        {
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var userId = request.UserId ?? "anonymous";

            _logger.LogInformation("Processing chat request for session: {SessionId}", sessionId);

            // 1. Retrieve relevant documents
            var relevantDocs = request.IncludeContext
                ? await RetrieveRelevantDocumentsAsync(request.Message, topK: 5)
                : new List<RetrievedDocument>();

            // 2. Prepare context for LLM
            var contextTexts = relevantDocs
                .Select(d => $"{d.Source}: {d.Content}")
                .ToList();

            // 3. Generate response using Ollama
            var response = await _ollamaService.GenerateChatResponseAsync(
                request.Message,
                contextTexts.Any() ? contextTexts : null
            );

            // 4. Save chat history
            var chatHistory = new ChatHistory
            {
                SessionId = sessionId,
                UserId = userId,
                UserMessage = request.Message,
                AssistantResponse = response,
                Context = relevantDocs.Any() ? JsonSerializer.Serialize(relevantDocs) : null,
                TokensUsed = EstimateTokens(request.Message + response),
                ChatTime = DateTime.UtcNow
            };

            await _chatHistoryRepository.AddAsync(chatHistory);

            _logger.LogInformation("Chat completed successfully");

            return new ChatResponse
            {
                Response = response,
                SessionId = sessionId,
                Context = relevantDocs,
                TokensUsed = chatHistory.TokensUsed,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            throw;
        }
    }

    public async Task<List<RetrievedDocument>> RetrieveRelevantDocumentsAsync(string query, int topK = 5)
    {
        try
        {
            // Generate embedding for query
            var queryEmbedding = await _ollamaService.GenerateEmbeddingAsync(query);

            if (queryEmbedding == null || !queryEmbedding.Any())
            {
                _logger.LogWarning("Empty query embedding, returning no documents");
                return new List<RetrievedDocument>();
            }

            // Search similar documents
            var similarDocs = await _documentRepository.SearchSimilarAsync(queryEmbedding, topK);

            var retrievedDocs = similarDocs.Select(doc => new RetrievedDocument
            {
                Source = doc.DocumentName,
                Content = doc.Content.Length > 1000 ? doc.Content.Substring(0, 1000) + "..." : doc.Content,
                Similarity = 0.85, // Would be calculated from actual similarity
                Metadata = doc.Metadata
            }).ToList();

            _logger.LogInformation("Retrieved {Count} relevant documents", retrievedDocs.Count);

            return retrievedDocs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relevant documents");
            return new List<RetrievedDocument>();
        }
    }

    public async Task<bool> IndexDocumentAsync(string content, string documentName, string documentType, string? metadata = null)
    {
        try
        {
            _logger.LogInformation("Indexing document: {DocumentName}", documentName);

            // Split content into chunks if too large
            var chunks = SplitIntoChunks(content, maxChunkSize: 1000);

            foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
            {
                var embedding = await _ollamaService.GenerateEmbeddingAsync(chunk);

                var document = new DocumentEmbedding
                {
                    DocumentName = $"{documentName}_chunk_{index}",
                    DocumentType = documentType,
                    Content = chunk,
                    Embedding = embedding,
                    Metadata = metadata,
                    IndexedAt = DateTime.UtcNow
                };

                await _documentRepository.AddAsync(document);
            }

            _logger.LogInformation("Successfully indexed document with {ChunkCount} chunks", chunks.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document: {DocumentName}", documentName);
            return false;
        }
    }

    public async Task<bool> IndexDatabaseRecordAsync(string tableName, int recordId, Dictionary<string, object> data)
    {
        try
        {
            _logger.LogInformation("Indexing database record: {TableName}:{RecordId}", tableName, recordId);

            // Convert record data to text
            var content = string.Join("\n", data.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            var embedding = await _ollamaService.GenerateEmbeddingAsync(content);

            var document = new DocumentEmbedding
            {
                DocumentName = $"{tableName}_{recordId}",
                DocumentType = "Database",
                Content = content,
                SourceTable = tableName,
                SourceRecordId = recordId,
                Embedding = embedding,
                Metadata = JsonSerializer.Serialize(data),
                IndexedAt = DateTime.UtcNow
            };

            // Delete existing embedding for this record
            await _documentRepository.DeleteBySourceAsync(tableName, recordId);

            // Add new embedding
            await _documentRepository.AddAsync(document);

            _logger.LogInformation("Successfully indexed database record");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing database record");
            return false;
        }
    }

    private List<string> SplitIntoChunks(string text, int maxChunkSize = 1000)
    {
        var chunks = new List<string>();
        var words = text.Split(' ');
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var word in words)
        {
            if (currentLength + word.Length > maxChunkSize && currentChunk.Any())
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(word);
            currentLength += word.Length + 1;
        }

        if (currentChunk.Any())
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }

    private int EstimateTokens(string text)
    {
        // Rough estimation: ~4 characters per token
        return text.Length / 4;
    }
}
