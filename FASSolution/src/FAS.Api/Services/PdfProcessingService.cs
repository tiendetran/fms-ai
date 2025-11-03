using FAS.Core.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

namespace FAS.Api.Services;

public interface IPdfProcessingService
{
    Task<string> ExtractTextFromPdfAsync(string filePath);
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    Task ProcessAndStorePdfAsync(string filePath, string documentId);
    Task<List<PdfDocument>> GetAllPdfDocumentsAsync();
}

public class PdfProcessingService : IPdfProcessingService
{
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<PdfProcessingService> _logger;
    private readonly string _uploadPath;

    public PdfProcessingService(
        IVectorStoreService vectorStoreService,
        IDatabaseContext dbContext,
        IConfiguration configuration,
        ILogger<PdfProcessingService> logger)
    {
        _vectorStoreService = vectorStoreService;
        _dbContext = dbContext;
        _logger = logger;
        _uploadPath = configuration.GetValue<string>("PDFSettings:UploadPath") ?? "PDFs";

        // Create upload directory if not exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF: {FilePath}", filePath);

            using var pdfReader = new PdfReader(filePath);
            var text = new StringBuilder();

            for (int page = 1; page <= pdfReader.NumberOfPages; page++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(pdfReader, page);
                text.AppendLine(pageText);
            }

            _logger.LogInformation("Extracted {Length} characters from PDF", text.Length);
            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF stream");

            using var pdfReader = new PdfReader(pdfStream);
            var text = new StringBuilder();

            for (int page = 1; page <= pdfReader.NumberOfPages; page++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(pdfReader, page);
                text.AppendLine(pageText);
            }

            return await Task.FromResult(text.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF stream");
            throw;
        }
    }

    public async Task ProcessAndStorePdfAsync(string filePath, string documentId)
    {
        try
        {
            _logger.LogInformation("Processing PDF: {DocumentId}", documentId);

            // Extract text
            var content = await ExtractTextFromPdfAsync(filePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("No text extracted from PDF: {DocumentId}", documentId);
                return;
            }

            // Split into chunks for better retrieval
            var chunks = SplitTextIntoChunks(content, maxChunkSize: 1000);

            // Store in PostgreSQL
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            // Create PDF documents table if not exists
            await Dapper.SqlMapper.ExecuteAsync(connection, @"
                CREATE TABLE IF NOT EXISTS pdf_documents (
                    id SERIAL PRIMARY KEY,
                    document_id VARCHAR(255) UNIQUE NOT NULL,
                    file_path VARCHAR(500),
                    file_name VARCHAR(255),
                    page_count INTEGER,
                    processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            ");

            var fileInfo = new FileInfo(filePath);
            await Dapper.SqlMapper.ExecuteAsync(connection, @"
                INSERT INTO pdf_documents (document_id, file_path, file_name, page_count)
                VALUES (@DocumentId, @FilePath, @FileName, @PageCount)
                ON CONFLICT (document_id) DO UPDATE 
                SET file_path = @FilePath, processed_at = CURRENT_TIMESTAMP;
            ", new
            {
                DocumentId = documentId,
                FilePath = filePath,
                FileName = fileInfo.Name,
                PageCount = chunks.Count
            });

            // Store chunks with embeddings
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkId = $"{documentId}_chunk_{i}";
                var metadata = new Dictionary<string, object>
                {
                    { "source_file", fileInfo.Name },
                    { "chunk_index", i },
                    { "total_chunks", chunks.Count }
                };

                await _vectorStoreService.StoreDocumentAsync(
                    chunkId,
                    chunks[i],
                    $"pdf:{filePath}",
                    metadata
                );
            }

            _logger.LogInformation(
                "PDF processed successfully: {DocumentId}, {ChunkCount} chunks stored",
                documentId, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<PdfDocument>> GetAllPdfDocumentsAsync()
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = @"
                SELECT 
                    document_id as DocumentId,
                    file_name as FileName,
                    file_path as FilePath,
                    page_count as PageCount,
                    processed_at as ProcessedAt
                FROM pdf_documents
                ORDER BY processed_at DESC";

            var documents = await Dapper.SqlMapper.QueryAsync<PdfDocument>(connection, sql);
            return documents.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF documents");
            throw;
        }
    }

    private List<string> SplitTextIntoChunks(string text, int maxChunkSize = 1000)
    {
        var chunks = new List<string>();
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
                continue;

            if (currentChunk.Length + trimmedSentence.Length > maxChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }

            currentChunk.Append(trimmedSentence).Append(". ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}

public class PdfDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public DateTime ProcessedAt { get; set; }
}
