using FAS.Core.DTOs;
using FAS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FAS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IRagService ragService, ILogger<ChatController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Chat với AI về dữ liệu FMS
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ChatResponse>>> Chat([FromBody] ChatRequestModel request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(ApiResponse<ChatResponse>.FailureResult("Message không được để trống"));
            }

            _logger.LogInformation("Processing chat request: {Message}", request.Message);

            var response = await _ragService.ChatAsync(request);

            return Ok(ApiResponse<ChatResponse>.SuccessResult(response, "Chat thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, ApiResponse<ChatResponse>.FailureResult("Lỗi xử lý chat", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Tìm kiếm tài liệu liên quan
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<ApiResponse<List<RetrievedDocument>>>> SearchDocuments([FromBody] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(ApiResponse<List<RetrievedDocument>>.FailureResult("Query không được để trống"));
            }

            _logger.LogInformation("Searching documents for query: {Query}", query);

            var documents = await _ragService.RetrieveRelevantDocumentsAsync(query, topK: 10);

            return Ok(ApiResponse<List<RetrievedDocument>>.SuccessResult(documents, $"Tìm thấy {documents.Count} tài liệu"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, ApiResponse<List<RetrievedDocument>>.FailureResult("Lỗi tìm kiếm tài liệu", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Hỏi về phiếu nhập nguyên liệu
    /// </summary>
    [HttpPost("material-receipts")]
    public async Task<ActionResult<ApiResponse<ChatResponse>>> AskMaterialReceipts([FromBody] ChatRequestModel request)
    {
        try
        {
            request.Message = $"Câu hỏi về phiếu nhập nguyên liệu: {request.Message}";
            var response = await _ragService.ChatAsync(request);
            return Ok(ApiResponse<ChatResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking about material receipts");
            return StatusCode(500, ApiResponse<ChatResponse>.FailureResult("Lỗi xử lý câu hỏi", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Hỏi về phiếu xuất bán hàng
    /// </summary>
    [HttpPost("sales-orders")]
    public async Task<ActionResult<ApiResponse<ChatResponse>>> AskSalesOrders([FromBody] ChatRequestModel request)
    {
        try
        {
            request.Message = $"Câu hỏi về đơn hàng bán: {request.Message}";
            var response = await _ragService.ChatAsync(request);
            return Ok(ApiResponse<ChatResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking about sales orders");
            return StatusCode(500, ApiResponse<ChatResponse>.FailureResult("Lỗi xử lý câu hỏi", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Hỏi về tồn kho
    /// </summary>
    [HttpPost("inventory")]
    public async Task<ActionResult<ApiResponse<ChatResponse>>> AskInventory([FromBody] ChatRequestModel request)
    {
        try
        {
            request.Message = $"Câu hỏi về tồn kho: {request.Message}";
            var response = await _ragService.ChatAsync(request);
            return Ok(ApiResponse<ChatResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking about inventory");
            return StatusCode(500, ApiResponse<ChatResponse>.FailureResult("Lỗi xử lý câu hỏi", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Tạo báo cáo tổng hợp
    /// </summary>
    [HttpPost("generate-report")]
    public async Task<ActionResult<ApiResponse<ChatResponse>>> GenerateReport([FromBody] string reportRequest)
    {
        try
        {
            var request = new ChatRequestModel
            {
                Message = $"Tạo báo cáo: {reportRequest}",
                IncludeContext = true
            };

            var response = await _ragService.ChatAsync(request);
            return Ok(ApiResponse<ChatResponse>.SuccessResult(response, "Báo cáo đã được tạo"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, ApiResponse<ChatResponse>.FailureResult("Lỗi tạo báo cáo", new List<string> { ex.Message }));
        }
    }
}
