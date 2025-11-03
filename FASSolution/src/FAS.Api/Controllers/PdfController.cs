using FAS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FAS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfProcessingService _pdfService;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PdfController> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly string _uploadPath;

    public PdfController(
        IPdfProcessingService pdfService,
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var pdfSettings = configuration.GetSection("PDFSettings");
        _maxFileSizeBytes = pdfSettings.GetValue<long>("MaxFileSizeMB", 50) * 1024 * 1024;
        _uploadPath = pdfSettings["UploadPath"] ?? "PDFs";

        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    /// <summary>
    /// Upload và xử lý PDF file
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            if (file.Length > _maxFileSizeBytes)
            {
                return BadRequest(new { message = $"File size exceeds maximum limit of {_maxFileSizeBytes / (1024 * 1024)}MB" });
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only PDF files are allowed" });
            }

            var documentId = Guid.NewGuid().ToString();
            var fileName = $"{documentId}_{file.FileName}";
            var filePath = Path.Combine(_uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("PDF uploaded: {FileName}", fileName);

            // Queue PDF processing as background task with proper service scope
            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                using var scope = _serviceProvider.CreateScope();
                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfProcessingService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PdfController>>();

                try
                {
                    await pdfService.ProcessAndStorePdfAsync(filePath, documentId);
                    logger.LogInformation("PDF processed successfully: {DocumentId}", documentId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing PDF: {DocumentId}", documentId);
                }
            });

            return Ok(new
            {
                documentId,
                fileName = file.FileName,
                message = "PDF uploaded successfully and is being processed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF");
            return StatusCode(500, new { message = "An error occurred while uploading PDF" });
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả PDF documents
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetAllPdfs()
    {
        try
        {
            var documents = await _pdfService.GetAllPdfDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF documents");
            return StatusCode(500, new { message = "An error occurred while getting PDF documents" });
        }
    }

    /// <summary>
    /// Xử lý lại một PDF document
    /// </summary>
    [HttpPost("reprocess/{documentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReprocessPdf(string documentId)
    {
        try
        {
            var documents = await _pdfService.GetAllPdfDocumentsAsync();
            var document = documents.FirstOrDefault(d => d.DocumentId == documentId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound(new { message = "PDF file not found on disk" });
            }

            await _pdfService.ProcessAndStorePdfAsync(document.FilePath, documentId);

            return Ok(new { message = "PDF reprocessed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing PDF: {DocumentId}", documentId);
            return StatusCode(500, new { message = "An error occurred while reprocessing PDF" });
        }
    }
}
