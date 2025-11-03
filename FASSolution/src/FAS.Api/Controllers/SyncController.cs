using FAS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FAS.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly IDatabaseSyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IDatabaseSyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Đồng bộ tất cả bảng từ SQL Server sang PostgreSQL
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> SyncAllTables()
    {
        try
        {
            _logger.LogInformation("Manual sync all tables requested");
            await _syncService.SyncAllTablesAsync();
            return Ok(new { message = "All tables synced successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all tables");
            return StatusCode(500, new { message = "An error occurred during sync", error = ex.Message });
        }
    }

    /// <summary>
    /// Đồng bộ một bảng cụ thể
    /// </summary>
    [HttpPost("table/{tableName}")]
    public async Task<IActionResult> SyncTable(string tableName)
    {
        try
        {
            _logger.LogInformation("Manual sync requested for table: {TableName}", tableName);
            await _syncService.SyncTableAsync(tableName);
            return Ok(new { message = $"Table {tableName} synced successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing table: {TableName}", tableName);
            return StatusCode(500, new { message = $"An error occurred syncing table {tableName}", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy trạng thái đồng bộ
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            var status = await _syncService.GetSyncStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            return StatusCode(500, new { message = "An error occurred getting sync status" });
        }
    }
}
