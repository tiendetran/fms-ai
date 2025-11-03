using FAS.Core.DTOs;
using FAS.Core.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;

namespace FAS.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly IRagService _ragService;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IRagService ragService, ILogger<PdfService> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    public async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("PDF file not found: {FilePath}", filePath);
                return string.Empty;
            }

            using var pdfReader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(pdfReader);

            var text = new System.Text.StringBuilder();

            for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
            {
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                text.AppendLine(pageText);
                text.AppendLine(); // Separator between pages
            }

            _logger.LogInformation("Successfully extracted text from PDF");
            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> IndexPdfAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Indexing PDF: {FilePath}", filePath);

            var text = await ExtractTextFromPdfAsync(filePath);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from PDF: {FilePath}", filePath);
                return false;
            }

            var fileName = Path.GetFileName(filePath);
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                FilePath = filePath,
                FileSize = new FileInfo(filePath).Length,
                IndexedDate = DateTime.UtcNow
            });

            var result = await _ragService.IndexDocumentAsync(
                content: text,
                documentName: fileName,
                documentType: "PDF",
                metadata: metadata
            );

            _logger.LogInformation("PDF indexed: {Success}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing PDF: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<List<SyncStatusDto>> SyncPdfFolderAsync(string folderPath)
    {
        var results = new List<SyncStatusDto>();

        try
        {
            _logger.LogInformation("Syncing PDF folder: {FolderPath}", folderPath);

            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Folder not found: {FolderPath}", folderPath);
                return results;
            }

            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);

            _logger.LogInformation("Found {Count} PDF files", pdfFiles.Length);

            foreach (var pdfFile in pdfFiles)
            {
                var status = new SyncStatusDto
                {
                    EntityType = $"PDF: {Path.GetFileName(pdfFile)}",
                    LastSyncTime = DateTime.UtcNow
                };

                try
                {
                    var success = await IndexPdfAsync(pdfFile);
                    status.IsSuccess = success;
                    status.RecordsSynced = success ? 1 : 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PDF: {FilePath}", pdfFile);
                    status.IsSuccess = false;
                    status.ErrorMessage = ex.Message;
                }

                results.Add(status);
            }

            _logger.LogInformation("PDF folder sync completed. Total: {Total}, Success: {Success}",
                results.Count, results.Count(r => r.IsSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing PDF folder: {FolderPath}", folderPath);
        }

        return results;
    }
}
