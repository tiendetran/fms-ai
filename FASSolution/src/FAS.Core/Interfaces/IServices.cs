using FAS.Core.DTOs;
using FAS.Core.Entities;
using OllamaSharp.Models.Chat;

namespace FAS.Core.Interfaces;

/// <summary>
/// Service cho Ollama AI
/// </summary>
//public interface IOllamaService
//{
//    Task<string> GenerateChatResponseAsync(string prompt, List<string>? context = null);
//    Task<float[]> GenerateEmbeddingAsync(string text);
//    Task<bool> TestConnectionAsync();
//}

public interface IOllamaService
{
    Task<string> ChatAsync(string prompt, List<Message>? conversationHistory = null);
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<bool> IsModelAvailableAsync();
}

/// <summary>
/// Service cho RAG
/// </summary>
public interface IRagService
{
    Task<ChatResponse> ChatAsync(ChatRequestModel request);
    Task<List<RetrievedDocument>> RetrieveRelevantDocumentsAsync(string query, int topK = 5);
    Task<bool> IndexDocumentAsync(string content, string documentName, string documentType, string? metadata = null);
    Task<bool> IndexDatabaseRecordAsync(string tableName, int recordId, Dictionary<string, object> data);
}

/// <summary>
/// Service đồng bộ dữ liệu
/// </summary>
public interface ISyncService
{
    Task<SyncStatusDto> SyncMaterialReceiptsAsync();
    Task<SyncStatusDto> SyncProductReceiptsAsync();
    Task<SyncStatusDto> SyncSalesOrdersAsync();
    Task<SyncStatusDto> SyncInventoryAsync();
    Task<SyncStatusDto> SyncMasterDataAsync();
    Task<List<SyncStatusDto>> SyncAllAsync();
}

/// <summary>
/// Service xử lý PDF
/// </summary>
public interface IPdfService
{
    Task<string> ExtractTextFromPdfAsync(string filePath);
    Task<bool> IndexPdfAsync(string filePath);
    Task<List<SyncStatusDto>> SyncPdfFolderAsync(string folderPath);
}

/// <summary>
/// Service JWT Authentication
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<string?> AuthenticateAsync(string username, string password);
    string GenerateJwtToken(dynamic user);
    Task<bool> ValidateTokenAsync(string token);
}

/// <summary>
/// Query Service cho FMS
/// </summary>
public interface IFmsQueryService
{
    Task<PagedResult<MaterialReceipt>> GetMaterialReceiptsAsync(MaterialReceiptQueryDto query);
    Task<List<Inventory>> GetInventoryAsync(InventoryQueryDto query);
    Task<object> GenerateSalesReportAsync(SalesReportDto query);
    Task<object> GetDashboardSummaryAsync();
}
