using FAS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FAS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IQueryService _queryService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(IQueryService queryService, ILogger<QueryController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// Truy vấn hệ thống FMS với RAG
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Query([FromBody] QueryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { message = "Query is required" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            request.UserId = userId;

            var result = await _queryService.QueryAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return StatusCode(500, new { message = "An error occurred while processing your query" });
        }
    }

    /// <summary>
    /// Lấy gợi ý cho câu truy vấn
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Query parameter 'q' is required" });
            }

            var suggestions = await _queryService.GetSuggestionsAsync(q);
            return Ok(new { suggestions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { message = "An error occurred while getting suggestions" });
        }
    }

    /// <summary>
    /// Ví dụ các câu hỏi thường gặp
    /// </summary>
    [HttpGet("examples")]
    public IActionResult GetExamples()
    {
        var examples = new[]
        {
            "Cho tôi biết danh sách nhập nguyên liệu trong tháng này",
            "Tồn kho nguyên liệu hiện tại như thế nào?",
            "Có bao nhiêu đơn hàng đang chờ sản xuất?",
            "Thống kê xuất bán hàng tuần trước",
            "Danh sách các nhà cung cấp nguyên liệu",
            "Kế hoạch sản xuất trong tuần này",
            "Báo cáo chất lượng sản phẩm tháng trước",
            "Phiếu nhập thành phẩm ngày hôm qua"
        };

        return Ok(new { examples });
    }
}
