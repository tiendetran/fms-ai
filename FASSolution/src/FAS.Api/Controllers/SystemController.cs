using FAS.Api.Services;
using FAS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FAS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly IDatabaseContext _dbContext;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IOllamaService ollamaService,
        IDatabaseContext dbContext,
        IVectorStoreService vectorStoreService,
        ILogger<SystemController> logger)
    {
        _ollamaService = ollamaService;
        _dbContext = dbContext;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "FMS RAG API",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Kiểm tra trạng thái các services
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var status = new
            {
                api = "running",
                database = await CheckDatabaseAsync(),
                ollama = await CheckOllamaAsync(),
                vectorStore = await CheckVectorStoreAsync(),
                timestamp = DateTime.UtcNow
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system status");
            return StatusCode(500, new { message = "Error checking system status" });
        }
    }

    /// <summary>
    /// Initialize vector store (chạy một lần khi setup)
    /// </summary>
    [HttpPost("init-vector-store")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InitializeVectorStore()
    {
        try
        {
            _logger.LogInformation("Initializing vector store");
            await _vectorStoreService.InitializeVectorExtensionAsync();
            return Ok(new { message = "Vector store initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing vector store");
            return StatusCode(500, new { message = "Error initializing vector store", error = ex.Message });
        }
    }

    private async Task<string> CheckDatabaseAsync()
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();
            return "connected";
        }
        catch
        {
            return "disconnected";
        }
    }

    private async Task<string> CheckOllamaAsync()
    {
        try
        {
            var isAvailable = await _ollamaService.IsModelAvailableAsync();
            return isAvailable ? "available" : "models not found";
        }
        catch
        {
            return "unavailable";
        }
    }

    private async Task<string> CheckVectorStoreAsync()
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            var sql = "SELECT COUNT(*) FROM document_embeddings";
            var count = await Dapper.SqlMapper.ExecuteScalarAsync<int>(connection, sql);

            return $"active ({count} documents)";
        }
        catch
        {
            return "not initialized";
        }
    }
}
