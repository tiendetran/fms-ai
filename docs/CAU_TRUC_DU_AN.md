# Tài Liệu Cấu Trúc Dự Án FMS-AI

## 1. TỔNG QUAN DỰ ÁN

**FMS-AI** là hệ thống quản lý nhà máy (Factory Management System) tích hợp công nghệ RAG (Retrieval-Augmented Generation) để cung cấp khả năng truy vấn thông minh và xử lý tài liệu tự động.

### Thông tin cơ bản
- **Tên dự án**: FMS-AI (Factory Management System with AI)
- **Nền tảng**: .NET 9.0 (ASP.NET Core Web API)
- **Ngôn ngữ**: C# với nullable reference types
- **Architecture**: Clean Architecture với phân tầng rõ ràng

---

## 2. CẤU TRÚC THỦ MỤC

```
/home/user/fms-ai/
├── FASSolution/
│   └── src/
│       ├── FAS.Api/                      # Tầng API - Web API Project
│       │   ├── Controllers/              # Các REST API endpoints
│       │   │   ├── AuthController.cs     # Xác thực người dùng
│       │   │   ├── ChatController.cs     # Chat với AI
│       │   │   ├── PdfController.cs      # Xử lý PDF
│       │   │   ├── QueryController.cs    # Truy vấn thông minh
│       │   │   ├── SyncController.cs     # Đồng bộ database
│       │   │   └── SystemController.cs   # Kiểm tra hệ thống
│       │   │
│       │   ├── Services/                 # Business logic services
│       │   ├── Middleware/               # Xử lý request pipeline
│       │   │   └── ErrorHandlingMiddleware.cs
│       │   │
│       │   ├── Data/                     # Database context
│       │   │   └── DatabaseContext.cs    # Quản lý kết nối DB
│       │   │
│       │   ├── Properties/               # Cài đặt project
│       │   ├── Program.cs                # Entry point, DI configuration
│       │   ├── appsettings.json          # Cấu hình chính
│       │   └── appsettings.Development.json
│       │
│       ├── FAS.Core/                     # Tầng Domain - Core logic
│       │   ├── DTOs/                     # Data Transfer Objects
│       │   │   └── CommonDtos.cs         # DTOs dùng chung
│       │   │
│       │   ├── Entities/                 # Domain entities
│       │   │   ├── FmsEntities.cs        # Entities của FMS
│       │   │   └── RagEntities.cs        # Entities của RAG
│       │   │
│       │   └── Interfaces/               # Service contracts
│       │       ├── IAuthService.cs
│       │       ├── IRagService.cs
│       │       ├── IOllamaService.cs
│       │       ├── IVectorStoreService.cs
│       │       └── ...
│       │
│       └── FAS.Infrastructure/           # Tầng Infrastructure
│           ├── Repositories/             # Data access implementations
│           │   ├── BaseRepository.cs     # Generic repository
│           │   ├── DocumentEmbeddingRepository.cs
│           │   └── ChatHistoryRepository.cs
│           │
│           ├── Services/                 # Service implementations
│           │   ├── AuthService.cs
│           │   ├── RagService.cs
│           │   ├── OllamaService.cs
│           │   ├── VectorStoreService.cs
│           │   ├── PdfProcessingService.cs
│           │   ├── DatabaseSyncService.cs
│           │   ├── QueryService.cs
│           │   └── FMSAgentService.cs
│           │
│           └── Data/                     # Database operations
│
├── docs/                                 # Tài liệu dự án
│   ├── CAU_TRUC_DU_AN.md                # File này
│   └── CONTROLLER_WORKFLOWS.md           # Luồng workflow controllers
│
├── PDFs/                                 # Thư mục lưu PDF uploads
└── Logs/                                 # Log files của Serilog
```

---

## 3. CÔNG NGHỆ SỬ DỤNG

### 3.1. Framework & Runtime
- **.NET 9.0** - Framework chính
- **ASP.NET Core Web API** - Xây dựng REST API
- **C# 13** - Ngôn ngữ lập trình

### 3.2. Database
| Database | Mục đích | Công nghệ |
|----------|----------|-----------|
| **SQL Server** | Database nguồn (Legacy FMS) | Microsoft.Data.SqlClient v6.1.2 |
| **PostgreSQL 18** | Database đích + Vector store | Npgsql v9.0.4 |
| **pgvector** | Lưu trữ embeddings | pgvector extension v0.3.2 |

**ORM**: Dapper (Micro ORM nhẹ, hiệu năng cao)

### 3.3. AI/ML Stack
| Thành phần | Công nghệ | Mô hình |
|------------|-----------|---------|
| **LLM Provider** | Ollama | `gpt-oss:120b-cloud` |
| **Embedding Model** | Ollama | `nomic-embed-text` (768 dims) |
| **Agent Framework** | Microsoft Semantic Kernel | v1.66.0 |
| **Vector Search** | pgvector | Cosine similarity |

### 3.4. Authentication & Security
- **JWT Bearer Token** - System.IdentityModel.Tokens.Jwt v8.14.0
- **BCrypt** - Password hashing (BCrypt.Net-Next v4.0.3)
- **CORS** - Cross-Origin Resource Sharing enabled

### 3.5. Thư viện bổ sung
| Thư viện | Phiên bản | Mục đích |
|----------|-----------|----------|
| **iText 7** | Latest | Xử lý PDF (đọc, extract text) |
| **Serilog** | v9.0.0 | Logging có cấu trúc |
| **Swashbuckle** | v9.0.6 | Swagger/OpenAPI documentation |
| **OllamaSharp** | Latest | Client SDK cho Ollama |

---

## 4. KIẾN TRÚC TỔNG QUAN

### 4.1. Mô hình 3-Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    FAS.Api (API Layer)                       │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Controllers: AuthController, ChatController, etc.     │  │
│  │ Middleware: ErrorHandling, JWT Authentication        │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │ Depends on ↓
┌─────────────────────────────────────────────────────────────┐
│                  FAS.Core (Domain Layer)                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Interfaces: IAuthService, IRagService, etc.           │  │
│  │ Entities: Material, Product, DocumentEmbedding        │  │
│  │ DTOs: ChatRequest, ChatResponse, QueryResult          │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │ Implemented by ↓
┌─────────────────────────────────────────────────────────────┐
│            FAS.Infrastructure (Data Layer)                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Services: RagService, VectorStoreService, etc.        │  │
│  │ Repositories: DocumentEmbeddingRepository             │  │
│  │ External Integrations: Ollama, PostgreSQL             │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 4.2. Luồng dữ liệu chính

```
┌──────────┐      ┌──────────────┐      ┌──────────────┐
│  Client  │─────▶│  Controller  │─────▶│   Service    │
│ (Web/App)│      │   (API)      │      │  (Business)  │
└──────────┘      └──────────────┘      └──────┬───────┘
                                               │
                  ┌────────────────────────────┘
                  │
     ┌────────────▼────────────┬──────────────┐
     │                         │              │
┌────▼─────┐         ┌────────▼─────┐  ┌────▼────────┐
│PostgreSQL│         │  SQL Server  │  │   Ollama    │
│ (Modern) │         │  (Legacy)    │  │    (AI)     │
└──────────┘         └──────────────┘  └─────────────┘
```

---

## 5. CÁC CONTROLLER CHÍNH

### Danh sách Controllers và chức năng

| Controller | Đường dẫn | Chức năng chính |
|------------|-----------|-----------------|
| **AuthController** | `/api/auth/*` | Xác thực người dùng, quản lý JWT token |
| **ChatController** | `/api/chat/*` | Chat với AI, tìm kiếm tài liệu, truy vấn FMS |
| **PdfController** | `/api/pdf/*` | Upload, xử lý, lưu trữ PDF với embeddings |
| **QueryController** | `/api/query/*` | Truy vấn thông minh với FMS Agent |
| **SyncController** | `/api/sync/*` | Đồng bộ dữ liệu từ SQL Server sang PostgreSQL |
| **SystemController** | `/api/system/*` | Health check, kiểm tra trạng thái hệ thống |

### Phân quyền truy cập

| Endpoint | Quyền truy cập |
|----------|----------------|
| `POST /api/auth/login` | Public (Không cần JWT) |
| `GET /api/system/health` | Public |
| `POST /api/sync/*` | **Admin only** |
| `POST /api/system/init-vector-store` | **Admin only** |
| Tất cả endpoints khác | **Authenticated** (Cần JWT token) |

---

## 6. DOMAIN MODELS (ENTITIES)

### 6.1. FMS Entities (Legacy từ SQL Server)
File: `FAS.Core/Entities/FmsEntities.cs`

| Entity | Bảng | Mô tả |
|--------|------|-------|
| **Material** | `tbl_GBMaterial` | Nguyên liệu, vật tư |
| **Product** | `tbl_Product` | Sản phẩm thành phẩm |
| **Unit** | `tbl_GBXNKUnit` | Đơn vị tính (kg, m, cái...) |
| **Supplier** | `tbl_GBVendor` | Nhà cung cấp |
| **Manufacturer** | `tbl_GBXNKManufacture` | Nhà sản xuất |
| **Customer** | `tbl_Customer` | Khách hàng |
| **Inventory** | `tbl_Inventory` | Tồn kho |
| **SalesOrder** | `tbl_SalesOrder` | Đơn hàng bán |
| **SalesDelivery** | `tbl_SalesDelivery` | Phiếu giao hàng |
| **MaterialReceipt** | `tbl_GBXNKPO` | Phiếu nhập nguyên liệu |
| **WorkOrder** | `tbl_WorkOrder` | Lệnh sản xuất |
| **ProductionPlan** | `tbl_ProductionPlan` | Kế hoạch sản xuất |

### 6.2. RAG Entities (PostgreSQL)
File: `FAS.Core/Entities/RagEntities.cs`

| Entity | Bảng | Mô tả |
|--------|------|-------|
| **DocumentEmbedding** | `document_embeddings` | Lưu vector embeddings của tài liệu |
| **ChatHistoryModel** | `chat_history` | Lịch sử chat với AI |
| **PdfDocument** | `pdf_documents` | Metadata của file PDF đã upload |
| **QueryHistory** | `query_history` | Lịch sử truy vấn người dùng |

#### Document Embedding Schema
```sql
CREATE TABLE document_embeddings (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL UNIQUE,
    content TEXT NOT NULL,
    embedding vector(768),  -- pgvector type
    source VARCHAR(100),
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Index để tăng tốc vector search
CREATE INDEX idx_embedding_ivfflat
ON document_embeddings
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

---

## 7. DATA TRANSFER OBJECTS (DTOs)

File: `FAS.Core/DTOs/CommonDtos.cs`

### 7.1. Authentication DTOs
```csharp
// Request login
public class LoginRequest {
    public string Username { get; set; }
    public string Password { get; set; }
}

// Response sau khi login thành công
public class LoginResponse {
    public string Token { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### 7.2. Chat DTOs
```csharp
// Request chat
public class ChatRequestModel {
    public string Message { get; set; }          // Câu hỏi của user
    public string? SessionId { get; set; }       // Session để track context
    public string? UserId { get; set; }          // User ID để personalize
    public int TopK { get; set; } = 5;           // Số documents liên quan
}

// Response chat
public class ChatResponse {
    public string Answer { get; set; }           // Câu trả lời từ AI
    public List<RetrievedDocument> Context { get; set; }  // Tài liệu tham khảo
    public int TokensUsed { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string? SessionId { get; set; }
}

// Document được retrieve từ vector store
public class RetrievedDocument {
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public string Source { get; set; }
    public float SimilarityScore { get; set; }   // 0.0 - 1.0
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### 7.3. Query DTOs
```csharp
public class QueryRequest {
    public string Query { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
}

public class QueryResult {
    public string Answer { get; set; }
    public List<string> RelatedDocuments { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string? QueryType { get; set; }
}
```

### 7.4. System DTOs
```csharp
public class SystemStatusDto {
    public bool IsHealthy { get; set; }
    public DatabaseStatus DatabaseStatus { get; set; }
    public OllamaStatus OllamaStatus { get; set; }
    public VectorStoreStatus VectorStoreStatus { get; set; }
}

public class SyncStatusDto {
    public string TableName { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int RecordsSynced { get; set; }
    public bool IsSuccess { get; set; }
}
```

### 7.5. Generic Response Wrapper
```csharp
public class ApiResponse<T> {
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}
```

---

## 8. SERVICES LAYER

### 8.1. Core Services

#### **IAuthService** - Xác thực & Ủy quyền
```csharp
Task<LoginResponse?> LoginAsync(LoginRequest request);
Task<string?> AuthenticateAsync(string username, string password);
Task<bool> ValidateTokenAsync(string token);
string GenerateJwtToken(dynamic user);
```

**Chức năng**:
- Kiểm tra username/password với database
- Tạo JWT token với claims (UserID, Role, Username)
- Xác thực token validity
- BCrypt password hashing

---

#### **IRagService** - Retrieval-Augmented Generation
```csharp
Task<ChatResponse> ChatAsync(ChatRequestModel request);
Task<List<RetrievedDocument>> RetrieveRelevantDocumentsAsync(string query, int topK);
Task<bool> IndexDocumentAsync(string content, string documentName, string documentType);
Task<bool> IndexDatabaseRecordAsync(string tableName, int recordId, Dictionary<string, object> data);
```

**Chức năng**:
- **Retrieve**: Tìm tài liệu liên quan dựa trên vector similarity
- **Augment**: Bổ sung context vào prompt
- **Generate**: Gọi LLM để tạo câu trả lời
- **Index**: Lưu embeddings của documents và DB records

**Workflow RAG**:
```
User Query → Generate Embedding → Vector Search (Top K)
→ Retrieve Documents → Augment Prompt with Context
→ Call LLM → Save Chat History → Return Response
```

---

#### **IOllamaService** - LLM Integration
```csharp
Task<string> ChatAsync(string prompt, List<Message>? conversationHistory = null);
Task<float[]> GenerateEmbeddingAsync(string text);
Task<bool> IsModelAvailableAsync();
```

**Chức năng**:
- Gọi Ollama API để chat với model `gpt-oss:120b-cloud`
- Tạo embeddings 768-chiều với model `nomic-embed-text`
- Kiểm tra model availability

**Configuration**:
```json
{
  "Endpoint": "http://192.168.2.252:11434",
  "ChatModel": "gpt-oss:120b-cloud",
  "EmbeddingModel": "nomic-embed-text",
  "Temperature": 0.7,
  "MaxTokens": 4096
}
```

---

#### **IVectorStoreService** - Vector Database
```csharp
Task InitializeVectorExtensionAsync();
Task<int> StoreDocumentAsync(string documentId, string content, string source, Dictionary<string, object>? metadata);
Task<List<SearchResult>> SearchSimilarAsync(string query, int topK);
Task<bool> DeleteDocumentAsync(string documentId);
```

**Chức năng**:
- Khởi tạo pgvector extension trong PostgreSQL
- Lưu trữ documents với vector embeddings (768 dimensions)
- Tìm kiếm similarity với cosine distance
- IVFFLAT indexing để tăng tốc search

---

#### **IPdfProcessingService** - Xử lý PDF
```csharp
Task<string> ExtractTextFromPdfAsync(string filePath);
Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
Task ProcessAndStorePdfAsync(string filePath, string documentId);
Task<List<PdfDocument>> GetAllPdfDocumentsAsync();
```

**Chức năng**:
- Extract text từ PDF bằng iText 7
- Xử lý tất cả pages trong PDF
- Lưu trữ text vào vector store với embeddings
- Background processing với queue

---

#### **IDatabaseSyncService** - Đồng bộ Database
```csharp
Task SyncAllTablesAsync();
Task SyncTableAsync(string tableName);
Task<SyncStatus> GetSyncStatusAsync();
```

**Chức năng**:
- Đồng bộ 1 chiều từ SQL Server → PostgreSQL
- Batch processing (1000 records/lần)
- Type mapping giữa 2 databases
- Auto-sync theo schedule (30 phút)

**Bảng được sync**:
- Master data: Materials, Products, Vendors, Customers, Warehouses
- Transactions: POs, Sales Orders, Deliveries
- Production: Work Orders, Production Plans
- Inventory: Stock levels

---

#### **IFMSAgentService** - Intelligent Query Agent
```csharp
Task<string> ProcessQueryAsync(string userQuery, string? userId = null);
Task<AgentResponse> ProcessWithContextAsync(string userQuery, List<string> context);
```

**Chức năng**:
- Sử dụng Microsoft Semantic Kernel
- Phát hiện intent của câu hỏi (inventory, sales, materials...)
- Tự động tạo SQL queries
- Tổng hợp thông tin từ nhiều nguồn
- Trả lời bằng tiếng Việt

**Query Types hỗ trợ**:
- Tồn kho (inventory)
- Đơn hàng bán (sales orders)
- Nguyên liệu (materials)
- Nhà cung cấp/Khách hàng
- Sản xuất (production)
- Queries với date range

---

#### **IQueryService** - Query Processing
```csharp
Task<QueryResult> QueryAsync(QueryRequest request);
Task<List<string>> GetSuggestionsAsync(string partialQuery);
```

**Chức năng**:
- Kết hợp FMSAgentService và RagService
- Lưu query history
- Suggest câu hỏi tương tự từ lịch sử
- Trả về related documents và metrics

---

### 8.2. Background Services

#### **DatabaseSyncBackgroundService**
- Hosted service chạy background
- Tự động sync theo schedule (mặc định 30 phút)
- Có thể bật/tắt qua config `AutoSyncEnabled`

#### **QueuedHostedService**
- Xử lý background tasks từ queue
- Chủ yếu cho PDF processing
- Đảm bảo upload không block API response

---

## 9. REPOSITORY LAYER

### 9.1. Base Repository Pattern
```csharp
public interface IRepository<T> {
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}
```

### 9.2. Specialized Repositories

#### **IDocumentEmbeddingRepository**
```csharp
Task<IEnumerable<DocumentEmbedding>> SearchSimilarAsync(float[] queryEmbedding, int topK = 5);
Task<IEnumerable<DocumentEmbedding>> GetByDocumentTypeAsync(string documentType);
Task<bool> DeleteBySourceAsync(string sourceTable, int sourceRecordId);
```

**SQL Query ví dụ (Vector Similarity Search)**:
```sql
SELECT id, document_id, content, source, metadata,
       embedding <=> @queryEmbedding AS distance
FROM document_embeddings
WHERE is_deleted = FALSE
ORDER BY embedding <=> @queryEmbedding
LIMIT @topK;
```

#### **IChatHistoryRepository**
```csharp
Task AddAsync(ChatHistoryModel entity);
Task<IEnumerable<ChatHistoryModel>> GetBySessionAsync(string sessionId);
Task<IEnumerable<ChatHistoryModel>> GetByUserAsync(string userId);
```

#### **IQueryHistoryRepository**
```csharp
Task AddAsync(QueryHistory entity);
Task<IEnumerable<QueryHistory>> GetRecentQueriesAsync(int count);
Task<IEnumerable<string>> SearchSimilarQueriesAsync(string partialQuery);
```

---

## 10. MIDDLEWARE & CROSS-CUTTING CONCERNS

### 10.1. Error Handling Middleware
**File**: `FAS.Api/Middleware/ErrorHandlingMiddleware.cs`

```csharp
// Bắt tất cả exceptions và trả về JSON response
try {
    await _next(context);
} catch (Exception ex) {
    await HandleExceptionAsync(context, ex);
}
```

**HTTP Status Code Mapping**:
- `UnauthorizedAccessException` → 401 Unauthorized
- `ArgumentException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- Default → 500 Internal Server Error

**Error Response Format**:
```json
{
  "success": false,
  "message": "Error message",
  "errors": ["Detail 1", "Detail 2"]
}
```

### 10.2. JWT Authentication
**Configuration trong Program.cs**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])
            )
        };
    });
```

### 10.3. CORS Policy
```csharp
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 10.4. Logging với Serilog
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/fms-ai-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Log Levels**:
- **Debug**: Chi tiết development
- **Information**: Workflow bình thường
- **Warning**: Vấn đề không nghiêm trọng
- **Error**: Lỗi xử lý
- **Fatal**: Crash hệ thống

---

## 11. CONFIGURATION FILES

### 11.1. appsettings.json
**File**: `FAS.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR_SQL_SERVER;Database=FMS;User Id=sa;Password=xxx;TrustServerCertificate=True",
    "PostgreSQL": "Host=localhost;Port=5432;Database=fms_rag;Username=postgres;Password=xxx"
  },

  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "FMS-RAG-API",
    "Audience": "FMS-RAG-Client",
    "ExpiryInMinutes": 60
  },

  "OllamaSettings": {
    "Endpoint": "http://192.168.2.252:11434",
    "ChatModel": "gpt-oss:120b-cloud",
    "EmbeddingModel": "nomic-embed-text",
    "Temperature": 0.7,
    "MaxTokens": 4096
  },

  "SyncSettings": {
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30,
    "BatchSize": 1000
  },

  "PDFSettings": {
    "UploadPath": "PDFs",
    "MaxFileSizeMB": 50,
    "AllowedExtensions": [".pdf"]
  },

  "VectorSettings": {
    "Dimensions": 768,
    "SimilarityThreshold": 0.7,
    "TopK": 5,
    "IndexType": "ivfflat",
    "IndexLists": 100
  }
}
```

### 11.2. Các thông số quan trọng

| Setting | Giá trị mặc định | Mô tả |
|---------|------------------|-------|
| `JwtSettings:ExpiryInMinutes` | 60 | Thời gian sống của JWT token |
| `OllamaSettings:Temperature` | 0.7 | Độ "sáng tạo" của AI (0.0-1.0) |
| `OllamaSettings:MaxTokens` | 4096 | Số token tối đa trong response |
| `SyncSettings:SyncIntervalMinutes` | 30 | Chu kỳ auto-sync |
| `SyncSettings:BatchSize` | 1000 | Số records xử lý mỗi batch |
| `PDFSettings:MaxFileSizeMB` | 50 | Dung lượng PDF tối đa |
| `VectorSettings:Dimensions` | 768 | Số chiều của embedding vector |
| `VectorSettings:TopK` | 5 | Số documents liên quan trả về |

---

## 12. DATABASE SCHEMA

### 12.1. SQL Server (Legacy FMS)
**Database**: `FMS`

**Các bảng chính**:
- `tbl_GBMaterial` - Danh mục nguyên liệu
- `tbl_Product` - Danh mục sản phẩm
- `tbl_GBVendor` - Nhà cung cấp
- `tbl_Customer` - Khách hàng
- `tbl_GBXNKPO` - Phiếu nhập nguyên liệu
- `tbl_SalesOrder` - Đơn hàng bán
- `tbl_SalesDelivery` - Phiếu giao hàng
- `tbl_Inventory` - Tồn kho
- `tbl_WorkOrder` - Lệnh sản xuất
- `tbl_ProductionPlan` - Kế hoạch sản xuất

### 12.2. PostgreSQL (Modern + RAG)
**Database**: `fms_rag`

#### Bảng: document_embeddings
```sql
CREATE TABLE document_embeddings (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL UNIQUE,
    content TEXT NOT NULL,
    embedding vector(768),
    source VARCHAR(100),
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_embedding_ivfflat
ON document_embeddings
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

CREATE INDEX idx_document_source ON document_embeddings(source);
CREATE INDEX idx_created_at ON document_embeddings(created_at);
```

#### Bảng: chat_history
```sql
CREATE TABLE chat_history (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(100),
    user_id VARCHAR(100),
    user_message TEXT NOT NULL,
    ai_response TEXT NOT NULL,
    context_used JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_session_id ON chat_history(session_id);
CREATE INDEX idx_user_id ON chat_history(user_id);
```

#### Bảng: query_history
```sql
CREATE TABLE query_history (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(100),
    query TEXT NOT NULL,
    query_type VARCHAR(50),
    response TEXT,
    processing_time_ms BIGINT,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_user_query ON query_history(user_id, created_at);
```

#### Bảng: pdf_documents
```sql
CREATE TABLE pdf_documents (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL UNIQUE,
    file_name VARCHAR(500) NOT NULL,
    file_path VARCHAR(1000),
    file_size_bytes BIGINT,
    page_count INT,
    upload_user_id VARCHAR(100),
    processing_status VARCHAR(50),
    created_at TIMESTAMP DEFAULT NOW(),
    processed_at TIMESTAMP
);
```

#### Bảng: sync_status
```sql
CREATE TABLE sync_status (
    id SERIAL PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL UNIQUE,
    last_sync_time TIMESTAMP,
    records_synced INT,
    is_success BOOLEAN,
    error_message TEXT,
    next_sync_time TIMESTAMP
);
```

---

## 13. SECURITY & BEST PRACTICES

### 13.1. Authentication Flow
```
1. User gửi POST /api/auth/login với username/password
2. AuthService kiểm tra credentials với DB
3. Nếu hợp lệ: Generate JWT token (60 phút expiry)
4. Client lưu token và gửi trong header: Authorization: Bearer {token}
5. Middleware tự động validate token cho mọi request
6. Token expired → Client phải login lại
```

### 13.2. Password Security
- **Hashing**: BCrypt với salt tự động
- **Không lưu plaintext password** trong database
- **Cost factor**: 10 (default BCrypt)

### 13.3. API Security
- **HTTPS recommended** (production)
- **JWT token trong Authorization header**
- **CORS configured** để control origins
- **Rate limiting** (nên thêm middleware)

### 13.4. Database Security
- **Connection strings** trong appsettings (không commit secrets)
- **Parameterized queries** với Dapper (chống SQL injection)
- **Least privilege**: User DB chỉ có quyền cần thiết

---

## 14. DEPLOYMENT & RUNNING

### 14.1. Prerequisites
1. **.NET 9.0 SDK** installed
2. **PostgreSQL 18** với pgvector extension
3. **SQL Server** (nếu sync data)
4. **Ollama** running với models:
   - `gpt-oss:120b-cloud`
   - `nomic-embed-text`

### 14.2. Setup Database
```sql
-- PostgreSQL
CREATE DATABASE fms_rag;
\c fms_rag
CREATE EXTENSION vector;

-- Chạy migrations (hoặc service sẽ tự tạo tables)
```

### 14.3. Configure appsettings.json
- Update connection strings
- Set JWT secret key (production: dùng Azure Key Vault hoặc environment variables)
- Configure Ollama endpoint

### 14.4. Build & Run
```bash
cd /home/user/fms-ai/FASSolution/src/FAS.Api

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

**API sẽ chạy tại**:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

**Swagger UI**: `http://localhost:5000/swagger`

### 14.5. Docker Deployment (Optional)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "FAS.Api.dll"]
```

---

## 15. MONITORING & MAINTENANCE

### 15.1. Logs
- **Location**: `/Logs/fms-ai-YYYY-MM-DD.txt`
- **Format**: Structured JSON logs
- **Rotation**: Daily rolling files

### 15.2. Health Checks
- **Endpoint**: `GET /api/system/health`
- **Checks**:
  - Database connectivity (SQL Server + PostgreSQL)
  - Ollama API availability
  - Vector store status

### 15.3. Performance Metrics
- Query processing time
- Vector search latency
- Document embedding time
- Chat response time

---

## 16. ROADMAP & FUTURE ENHANCEMENTS

### Potential Improvements
1. **Caching**: Redis cache cho frequent queries
2. **Rate Limiting**: Protect API từ abuse
3. **Advanced Auth**: Role-based access control (RBAC)
4. **Real-time**: SignalR cho chat streaming
5. **Multi-tenancy**: Support nhiều organizations
6. **Analytics Dashboard**: Query statistics, usage metrics
7. **Mobile App**: React Native hoặc Flutter client
8. **Export**: PDF reports, Excel exports

---

## 17. TROUBLESHOOTING

### Common Issues

#### 1. Ollama Connection Failed
```
Error: Unable to connect to Ollama at http://192.168.2.252:11434
```
**Solution**:
- Kiểm tra Ollama service đang chạy: `ollama list`
- Verify models installed: `ollama pull gpt-oss:120b-cloud`
- Check network connectivity

#### 2. Vector Search Returns No Results
**Solution**:
- Verify documents indexed: `SELECT COUNT(*) FROM document_embeddings WHERE is_deleted = FALSE`
- Check similarity threshold trong config
- Ensure pgvector extension enabled: `CREATE EXTENSION IF NOT EXISTS vector`

#### 3. JWT Token Validation Failed
**Solution**:
- Check token expiry time
- Verify JWT secret key matches between login và validation
- Ensure `Authorization: Bearer {token}` header format correct

#### 4. Database Sync Fails
**Solution**:
- Verify SQL Server connection string
- Check table exists trong source database
- Review type mappings trong DatabaseSyncService
- Check batch size không quá lớn

---

## 18. CONTACT & SUPPORT

### Documentation
- **Project**: FMS-AI RAG System
- **Repository**: (Internal)
- **API Docs**: Swagger UI tại `/swagger`

### Resources
- [.NET 9.0 Docs](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [pgvector Documentation](https://github.com/pgvector/pgvector)
- [Ollama API](https://github.com/ollama/ollama/blob/main/docs/api.md)
- [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)

---

**Cập nhật lần cuối**: 2025-11-03
