using FAS.Core.Entities;
using FAS.Core.Interfaces;

namespace FAS.Api.Services;

public interface IFMSAgentService
{
    Task<string> ProcessQueryAsync(string userQuery, string? userId = null);
    Task<AgentResponse> ProcessWithContextAsync(string userQuery, List<string> context);
}

public class FMSAgentService : IFMSAgentService
{
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IOllamaService _ollamaService;
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<FMSAgentService> _logger;
    private readonly Kernel _kernel;
    private readonly string _ollamaEndpoint;
    private readonly string _chatModel;

    public FMSAgentService(
        IVectorStoreService vectorStoreService,
        IOllamaService ollamaService,
        IDatabaseContext dbContext,
        IConfiguration configuration,
        ILogger<FMSAgentService> logger)
    {
        _vectorStoreService = vectorStoreService;
        _ollamaService = ollamaService;
        _dbContext = dbContext;
        _logger = logger;

        var ollamaSettings = configuration.GetSection("OllamaSettings");
        _ollamaEndpoint = ollamaSettings["Endpoint"] ?? "http://localhost:11434";
        _chatModel = ollamaSettings["ChatModel"] ?? "gpt-oss:120b-cloud";

        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(
            modelId: _chatModel,
            endpoint: new Uri(_ollamaEndpoint));

        _kernel = builder.Build();
        _logger.LogInformation("FMS Agent initialized with Semantic Kernel");
    }

    public async Task<string> ProcessQueryAsync(string userQuery, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Processing query: {Query}", userQuery);

            // Bước 1: Tìm kiếm context liên quan từ vector store
            var similarDocs = await _vectorStoreService.SearchSimilarAsync(userQuery, topK: 5);

            // Bước 2: Xác định loại query và truy vấn database nếu cần
            var queryType = DetermineQueryType(userQuery);
            var databaseResults = await ExecuteDatabaseQuery(userQuery, queryType);

            // Bước 3: Tạo context đầy đủ
            var contextBuilder = new System.Text.StringBuilder();

            contextBuilder.AppendLine("## Thông tin từ tài liệu:");
            foreach (var doc in similarDocs)
            {
                contextBuilder.AppendLine($"- {doc.Content}");
            }

            if (!string.IsNullOrEmpty(databaseResults))
            {
                contextBuilder.AppendLine("\n## Dữ liệu từ hệ thống:");
                contextBuilder.AppendLine(databaseResults);
            }

            // Bước 4: Tạo prompt với context
            var systemPrompt = @"
Bạn là trợ lý AI chuyên nghiệp cho hệ thống quản lý nhà máy FMS (Factory Management System).
Nhiệm vụ của bạn là trả lời các câu hỏi về:
- Nguyên liệu, thành phẩm
- Nhà cung cấp, nhà sản xuất, khách hàng
- Phiếu nhập xuất nguyên liệu
- Đơn hàng, kế hoạch sản xuất, lệnh sản xuất
- Kiểm tra chất lượng (LAB, QC, NIR)
- Tồn kho, xuất bán hàng
- Báo cáo và thống kê

Hãy trả lời chính xác, ngắn gọn và dễ hiểu dựa trên thông tin được cung cấp.
Nếu không có đủ thông tin, hãy nói rõ điều đó.
";

            var fullPrompt = $@"{systemPrompt}

## Context (Thông tin tham khảo):
{contextBuilder}

## Câu hỏi của người dùng:
{userQuery}

## Trả lời:";

            // Bước 5: Gọi LLM để tạo câu trả lời
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(fullPrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            _logger.LogInformation("Query processed successfully");
            return response.Content ?? "Xin lỗi, tôi không thể xử lý câu hỏi này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            throw;
        }
    }

    public async Task<AgentResponse> ProcessWithContextAsync(string userQuery, List<string> context)
    {
        try
        {
            var contextText = string.Join("\n", context);
            var prompt = $@"
Context:
{contextText}

Question: {userQuery}

Answer based on the context provided:";

            var response = await _ollamaService.ChatAsync(prompt);

            return new AgentResponse
            {
                Answer = response,
                Context = context,
                Confidence = 0.8f // Có thể tính toán confidence score
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing with context");
            throw;
        }
    }

    private QueryType DetermineQueryType(string query)
    {
        var lowerQuery = query.ToLower();

        if (lowerQuery.Contains("nhập") && lowerQuery.Contains("nguyên liệu"))
            return QueryType.MaterialReceipt;

        if (lowerQuery.Contains("nhập") && lowerQuery.Contains("thành phẩm"))
            return QueryType.FinishedGoodsReceipt;

        if (lowerQuery.Contains("xuất") || lowerQuery.Contains("bán hàng"))
            return QueryType.Sales;

        if (lowerQuery.Contains("tồn kho"))
            return QueryType.Inventory;

        if (lowerQuery.Contains("đơn hàng"))
            return QueryType.SalesOrder;

        if (lowerQuery.Contains("kế hoạch") && lowerQuery.Contains("sản xuất"))
            return QueryType.ProductionPlan;

        if (lowerQuery.Contains("nhà cung cấp"))
            return QueryType.Vendor;

        if (lowerQuery.Contains("khách hàng"))
            return QueryType.Customer;

        if (lowerQuery.Contains("báo cáo"))
            return QueryType.Report;

        return QueryType.General;
    }

    private async Task<string> ExecuteDatabaseQuery(string userQuery, QueryType queryType)
    {
        try
        {
            using var connection = _dbContext.CreatePostgreSqlConnection();
            connection.Open();

            string sql = queryType switch
            {
                QueryType.MaterialReceipt => @"
                    SELECT po_number, material_name, quantity, unit, received_date, vendor_name
                    FROM tbl_gbxnkpo 
                    ORDER BY received_date DESC 
                    LIMIT 10",

                QueryType.Sales => @"
                    SELECT delivery_no, customer_name, product_name, quantity, delivery_date
                    FROM tbl_salesdelivery
                    ORDER BY delivery_date DESC
                    LIMIT 10",

                QueryType.Inventory => @"
                    SELECT warehouse_name, material_name, quantity, unit
                    FROM tbl_inventory
                    WHERE quantity > 0
                    ORDER BY warehouse_name, material_name
                    LIMIT 20",

                QueryType.SalesOrder => @"
                    SELECT order_no, customer_name, order_date, delivery_date, status
                    FROM tbl_salesorder
                    ORDER BY order_date DESC
                    LIMIT 10",

                _ => null
            };

            if (string.IsNullOrEmpty(sql))
                return string.Empty;

            var results = await Dapper.SqlMapper.QueryAsync(connection, sql);
            var resultsList = results.ToList();

            if (!resultsList.Any())
                return "Không tìm thấy dữ liệu.";

            // Format results as text
            var output = new System.Text.StringBuilder();
            foreach (var row in resultsList)
            {
                var dict = (IDictionary<string, object>)row;
                output.AppendLine(string.Join(", ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing database query for type: {QueryType}", queryType);
            return string.Empty;
        }
    }
}

public enum QueryType
{
    General,
    MaterialReceipt,
    FinishedGoodsReceipt,
    Sales,
    Inventory,
    SalesOrder,
    ProductionPlan,
    Vendor,
    Customer,
    Report
}

public class AgentResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<string> Context { get; set; } = new();
    public float Confidence { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
