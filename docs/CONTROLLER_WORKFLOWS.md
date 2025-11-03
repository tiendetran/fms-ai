# Tài Liệu Workflows của Controllers - FMS-AI

## MỤC LỤC
1. [AuthController - Xác thực người dùng](#1-authcontroller---xác-thực-người-dùng)
2. [ChatController - Chat với AI](#2-chatcontroller---chat-với-ai)
3. [PdfController - Xử lý PDF](#3-pdfcontroller---xử-lý-pdf)
4. [QueryController - Truy vấn thông minh](#4-querycontroller---truy-vấn-thông-minh)
5. [SyncController - Đồng bộ Database](#5-synccontroller---đồng-bộ-database)
6. [SystemController - Quản lý hệ thống](#6-systemcontroller---quản-lý-hệ-thống)

---

## 1. AuthController - Xác thực người dùng

**File**: `FASSolution/src/FAS.Api/Controllers/AuthController.cs`

### 1.1. Tổng quan
Controller chịu trách nhiệm xác thực người dùng và quản lý JWT tokens cho authorization.

### 1.2. Dependencies
```csharp
private readonly IAuthService _authService;
private readonly ILogger<AuthController> _logger;
```

### 1.3. Endpoints

---

#### **POST /api/auth/login**

**Mục đích**: Đăng nhập và nhận JWT token

**Authorization**: Public (không cần token)

**Request Body**:
```json
{
  "username": "admin",
  "password": "password123"
}
```

**Workflow chi tiết**:

```mermaid
sequenceDiagram
    participant Client
    participant AuthController
    participant AuthService
    participant Database
    participant BCrypt

    Client->>AuthController: POST /api/auth/login
    AuthController->>AuthController: Validate request (username, password not empty)

    alt Validation failed
        AuthController->>Client: 400 Bad Request
    end

    AuthController->>AuthService: LoginAsync(loginRequest)
    AuthService->>Database: SELECT user WHERE username = @username

    alt User not found
        Database->>AuthService: null
        AuthService->>AuthController: null
        AuthController->>Client: 401 Unauthorized (Invalid credentials)
    end

    Database->>AuthService: User object
    AuthService->>BCrypt: Verify(password, user.PasswordHash)

    alt Password incorrect
        BCrypt->>AuthService: false
        AuthService->>AuthController: null
        AuthController->>Client: 401 Unauthorized (Invalid credentials)
    end

    BCrypt->>AuthService: true (password match)
    AuthService->>AuthService: GenerateJwtToken(user)
    Note over AuthService: Claims: UserID, Username, FullName, Role, JTI<br/>Expiry: 60 minutes<br/>Algorithm: HMAC SHA-256

    AuthService->>AuthController: LoginResponse (token, username, role, expiresAt)
    AuthController->>Client: 200 OK + LoginResponse
```

**Response Success (200)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "fullName": "Administrator",
  "role": "Admin",
  "expiresAt": "2025-11-03T15:30:00Z"
}
```

**Response Error (401)**:
```json
{
  "message": "Invalid username or password"
}
```

**Code Flow**:
```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // 1. Validate input
    if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        return BadRequest(new { message = "Username and password are required" });

    // 2. Authenticate user
    var response = await _authService.LoginAsync(request);

    // 3. Check result
    if (response == null)
        return Unauthorized(new { message = "Invalid username or password" });

    // 4. Return token
    return Ok(response);
}
```

**Security Notes**:
- Password không bao giờ lưu dạng plaintext
- BCrypt tự động thêm salt
- JWT token có thời gian sống giới hạn (60 phút)
- Token chứa claims: UserID, Username, Role để authorization

---

#### **POST /api/auth/validate**

**Mục đích**: Kiểm tra JWT token có hợp lệ không

**Authorization**: Requires JWT token

**Request Header**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Workflow**:

```
1. Client gửi request với JWT token trong header
2. Middleware automatically validates token:
   - Check signature với secret key
   - Check expiry time
   - Check issuer & audience
3. Nếu token valid:
   - Request được phép pass qua
   - Controller trả về success
4. Nếu token invalid/expired:
   - Middleware trả về 401 Unauthorized
   - Request không reach controller
```

**Response Success (200)**:
```json
{
  "valid": true,
  "message": "Token is valid"
}
```

**Response Error (401)**:
```json
{
  "message": "Invalid or expired token"
}
```

---

### 1.4. JWT Token Structure

**Claims included**:
```json
{
  "sub": "12345",                          // UserID
  "unique_name": "admin",                  // Username
  "name": "Administrator",                 // Full Name
  "role": "Admin",                         // User Role
  "jti": "a1b2c3d4-e5f6-...",             // JWT ID (unique)
  "nbf": 1730649600,                       // Not Before
  "exp": 1730653200,                       // Expiry (60 mins later)
  "iat": 1730649600,                       // Issued At
  "iss": "FMS-RAG-API",                    // Issuer
  "aud": "FMS-RAG-Client"                  // Audience
}
```

**Token Generation Code**:
```csharp
public string GenerateJwtToken(dynamic user)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
        new Claim(JwtRegisteredClaimNames.Name, user.FullName ?? ""),
        new Claim(ClaimTypes.Role, user.Role ?? "User"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: _jwtIssuer,
        audience: _jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

### 1.5. Error Scenarios

| Scenario | HTTP Code | Response |
|----------|-----------|----------|
| Empty username/password | 400 | `{ message: "Username and password are required" }` |
| User không tồn tại | 401 | `{ message: "Invalid username or password" }` |
| Password sai | 401 | `{ message: "Invalid username or password" }` |
| Token expired | 401 | `{ message: "Invalid or expired token" }` |
| Token signature invalid | 401 | `{ message: "Invalid or expired token" }` |

---

## 2. ChatController - Chat với AI

**File**: `FASSolution/src/FAS.Api/Controllers/ChatController.cs`

### 2.1. Tổng quan
Controller cung cấp khả năng chat với AI về dữ liệu FMS sử dụng RAG (Retrieval-Augmented Generation).

### 2.2. Dependencies
```csharp
private readonly IRagService _ragService;
private readonly ILogger<ChatController> _logger;
```

### 2.3. Endpoints

---

#### **POST /api/chat**

**Mục đích**: Chat chung về dữ liệu FMS

**Authorization**: Requires JWT token

**Request Body**:
```json
{
  "message": "Hiện tại có bao nhiêu nguyên liệu trong kho?",
  "sessionId": "session-12345",
  "userId": "user-67890",
  "topK": 5
}
```

**Workflow chi tiết**:

```mermaid
sequenceDiagram
    participant Client
    participant ChatController
    participant RagService
    participant OllamaService
    participant VectorStore
    participant Database
    participant Ollama

    Client->>ChatController: POST /api/chat (message)
    ChatController->>RagService: ChatAsync(chatRequest)

    Note over RagService: Step 1: Generate Embedding
    RagService->>OllamaService: GenerateEmbeddingAsync(message)
    OllamaService->>Ollama: POST /api/embeddings (nomic-embed-text)
    Ollama->>OllamaService: float[768] embedding
    OllamaService->>RagService: embedding vector

    Note over RagService: Step 2: Vector Search
    RagService->>VectorStore: SearchSimilarAsync(embedding, topK=5)
    VectorStore->>Database: SELECT ... ORDER BY embedding <=> @vector LIMIT 5
    Database->>VectorStore: List<DocumentEmbedding>
    VectorStore->>RagService: List<RetrievedDocument>

    Note over RagService: Step 3: Augment Prompt
    RagService->>RagService: BuildPromptWithContext(message, documents)
    Note over RagService: Prompt = System Prompt + Context Documents + User Query

    Note over RagService: Step 4: Generate Response
    RagService->>OllamaService: ChatAsync(augmentedPrompt)
    OllamaService->>Ollama: POST /api/chat (gpt-oss:120b-cloud)
    Note over Ollama: AI processes with context<br/>and generates answer
    Ollama->>OllamaService: AI response
    OllamaService->>RagService: answer text

    Note over RagService: Step 5: Save History
    RagService->>Database: INSERT INTO chat_history
    Database->>RagService: Success

    RagService->>ChatController: ChatResponse (answer, context, metrics)
    ChatController->>Client: 200 OK + ChatResponse
```

**Prompt Augmentation Example**:
```text
System: Bạn là trợ lý AI của hệ thống quản lý nhà máy FMS. Hãy trả lời câu hỏi dựa trên context được cung cấp.

Context (Retrieved Documents):
---
Document 1 (Similarity: 0.89):
Nguồn: tbl_Inventory
Nội dung: Kho A hiện có 1500 kg thép không gỉ, 3000 kg nhôm...
---
Document 2 (Similarity: 0.85):
Nguồn: tbl_GBMaterial
Nội dung: Danh sách nguyên liệu: Thép không gỉ (MaterialID: 101), Nhôm (MaterialID: 102)...
---

User Question: Hiện tại có bao nhiêu nguyên liệu trong kho?

AI: Dựa trên dữ liệu từ kho, hiện tại có khoảng 4500 kg nguyên liệu gồm:
- 1500 kg thép không gỉ
- 3000 kg nhôm
(Dữ liệu cập nhật: 2025-11-03)
```

**Response Success (200)**:
```json
{
  "answer": "Dựa trên dữ liệu từ kho, hiện tại có khoảng 4500 kg nguyên liệu...",
  "context": [
    {
      "documentId": "doc-inv-001",
      "content": "Kho A hiện có 1500 kg thép không gỉ, 3000 kg nhôm...",
      "source": "tbl_Inventory",
      "similarityScore": 0.89,
      "metadata": {
        "lastUpdated": "2025-11-03"
      }
    }
  ],
  "tokensUsed": 523,
  "processingTimeMs": 2340,
  "sessionId": "session-12345"
}
```

---

#### **POST /api/chat/search**

**Mục đích**: Tìm kiếm documents liên quan (không generate answer)

**Authorization**: Requires JWT token

**Request**:
```json
{
  "message": "phiếu nhập nguyên liệu tháng 10",
  "topK": 10
}
```

**Workflow**:
```
1. Generate embedding cho query
2. Vector search trong document_embeddings
3. Trả về Top K documents (không gọi LLM)
```

**Response**:
```json
{
  "documents": [
    {
      "documentId": "po-2025-10-001",
      "content": "Phiếu nhập nguyên liệu số PO-2025-10-001...",
      "source": "tbl_GBXNKPO",
      "similarityScore": 0.92
    }
  ]
}
```

---

#### **POST /api/chat/material-receipts**

**Mục đích**: Chat về phiếu nhập nguyên liệu

**Đặc điểm**: Chỉ search trong documents có `source = "tbl_GBXNKPO"`

**Request**:
```json
{
  "message": "Nhà cung cấp nào giao nhiều nguyên liệu nhất tháng này?"
}
```

**Workflow**:
```
1. Filter documents WHERE source = 'tbl_GBXNKPO'
2. Generate embedding & search
3. Augment prompt with specialized system prompt for PO analysis
4. Generate answer
```

---

#### **POST /api/chat/sales-orders**

**Mục đích**: Chat về đơn hàng bán

**Đặc điểm**: Chỉ search trong `tbl_SalesOrder` và `tbl_SalesDelivery`

**Request**:
```json
{
  "message": "Tổng giá trị đơn hàng trong quý này là bao nhiêu?"
}
```

---

#### **POST /api/chat/inventory**

**Mục đích**: Chat về tồn kho

**Đặc điểm**: Focus trên `tbl_Inventory` và real-time stock data

**Request**:
```json
{
  "message": "Những nguyên liệu nào sắp hết hàng?"
}
```

---

#### **POST /api/chat/generate-report**

**Mục đích**: Tạo báo cáo tổng hợp

**Request**:
```json
{
  "message": "Tạo báo cáo tổng hợp tình hình sản xuất tháng 10"
}
```

**Đặc điểm**:
- Gọi multiple searches (production, inventory, sales)
- Aggregate data từ nhiều nguồn
- Format báo cáo theo template
- Thời gian xử lý lâu hơn (5-10 giây)

---

### 2.4. RAG Pipeline Explained

**Các bước chi tiết của RAG**:

1. **Embedding Generation** (200-300ms)
   - Input: User query text
   - Process: Call Ollama `nomic-embed-text` model
   - Output: 768-dimension float vector

2. **Vector Search** (50-100ms)
   - Input: Query embedding
   - Process: PostgreSQL pgvector cosine similarity search
   - Output: Top K most similar documents

3. **Context Assembly** (10ms)
   - Input: Retrieved documents
   - Process: Format documents into prompt context
   - Output: Augmented prompt

4. **LLM Generation** (1000-3000ms)
   - Input: Augmented prompt
   - Process: Call Ollama `gpt-oss:120b-cloud`
   - Output: AI-generated answer

5. **History Storage** (20ms)
   - Input: Query, answer, context
   - Process: INSERT INTO chat_history
   - Output: Success confirmation

**Total time**: ~2-4 seconds per query

---

### 2.5. Error Handling

| Error | HTTP Code | Response |
|-------|-----------|----------|
| Empty message | 400 | `{ message: "Message is required" }` |
| Ollama unavailable | 503 | `{ message: "AI service temporarily unavailable" }` |
| Vector store error | 500 | `{ message: "Failed to retrieve context" }` |
| LLM generation timeout | 504 | `{ message: "Request timeout, please try again" }` |

---

## 3. PdfController - Xử lý PDF

**File**: `FASSolution/src/FAS.Api/Controllers/PdfController.cs`

### 3.1. Tổng quan
Controller xử lý upload, extract text, và index PDF documents vào vector store.

### 3.2. Dependencies
```csharp
private readonly IPdfProcessingService _pdfProcessingService;
private readonly IBackgroundTaskQueue _taskQueue;
private readonly IServiceProvider _serviceProvider;
private readonly ILogger<PdfController> _logger;
```

### 3.3. Endpoints

---

#### **POST /api/pdf/upload**

**Mục đích**: Upload PDF file và xử lý background

**Authorization**: Requires JWT token

**Request**: multipart/form-data
```
file: [PDF binary]
```

**Workflow chi tiết**:

```mermaid
sequenceDiagram
    participant Client
    participant PdfController
    participant FileSystem
    participant BackgroundQueue
    participant Worker
    participant PdfService
    participant iText7
    participant OllamaService
    participant VectorStore
    participant Database

    Client->>PdfController: POST /api/pdf/upload (multipart)
    PdfController->>PdfController: Validate file (size, extension)

    alt Validation failed
        PdfController->>Client: 400 Bad Request
    end

    PdfController->>PdfController: Generate unique documentId (Guid)
    PdfController->>FileSystem: Save PDF to disk (PDFs/{documentId}.pdf)
    FileSystem->>PdfController: File path

    PdfController->>Database: INSERT INTO pdf_documents (metadata)
    Note over Database: Status: "Pending"

    PdfController->>BackgroundQueue: Enqueue(ProcessPdfTask)
    PdfController->>Client: 202 Accepted + documentId

    Note over Client: Client nhận response ngay<br/>Processing diễn ra background

    BackgroundQueue->>Worker: Dequeue task
    Worker->>PdfService: ProcessAndStorePdfAsync(filePath, documentId)

    PdfService->>iText7: Open PDF
    iText7->>PdfService: PdfDocument

    loop For each page
        PdfService->>iText7: Extract text from page
        iText7->>PdfService: Page text
        PdfService->>PdfService: Append to fullText
    end

    iText7->>PdfService: Close document
    PdfService->>PdfService: Clean text (remove special chars, trim)

    PdfService->>OllamaService: GenerateEmbeddingAsync(fullText)
    OllamaService->>PdfService: embedding vector

    PdfService->>VectorStore: StoreDocumentAsync(documentId, text, embedding)
    VectorStore->>Database: INSERT INTO document_embeddings
    Database->>VectorStore: Success

    PdfService->>Database: UPDATE pdf_documents SET status='Processed'
    PdfService->>Worker: Success

    Note over Client: Client có thể poll status<br/>hoặc xem trong /api/pdf/list
```

**Request Example (cURL)**:
```bash
curl -X POST http://localhost:5000/api/pdf/upload \
  -H "Authorization: Bearer {token}" \
  -F "file=@document.pdf"
```

**Response Success (202 Accepted)**:
```json
{
  "success": true,
  "message": "PDF uploaded and queued for processing",
  "data": {
    "documentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "fileName": "document.pdf",
    "status": "Pending"
  }
}
```

**Response Error (400)**:
```json
{
  "success": false,
  "message": "Invalid file",
  "errors": [
    "File size exceeds maximum allowed (50MB)",
    "Only PDF files are allowed"
  ]
}
```

**Validation Rules**:
```csharp
// 1. File not null
if (file == null || file.Length == 0)
    return BadRequest("No file uploaded");

// 2. File extension check
var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
if (extension != ".pdf")
    return BadRequest("Only PDF files allowed");

// 3. File size check
var maxSizeMB = _configuration.GetValue<int>("PDFSettings:MaxFileSizeMB");
if (file.Length > maxSizeMB * 1024 * 1024)
    return BadRequest($"File size exceeds {maxSizeMB}MB");
```

---

#### **GET /api/pdf/list**

**Mục đích**: Lấy danh sách tất cả PDF đã upload

**Authorization**: Requires JWT token

**Response**:
```json
{
  "documents": [
    {
      "documentId": "doc-001",
      "fileName": "manual.pdf",
      "fileSizeBytes": 2457600,
      "pageCount": 45,
      "uploadUserId": "user-123",
      "processingStatus": "Processed",
      "createdAt": "2025-11-01T10:30:00Z",
      "processedAt": "2025-11-01T10:32:15Z"
    },
    {
      "documentId": "doc-002",
      "fileName": "report.pdf",
      "processingStatus": "Pending",
      "createdAt": "2025-11-03T14:20:00Z"
    }
  ]
}
```

**Workflow**:
```
1. PdfController gọi PdfProcessingService.GetAllPdfDocumentsAsync()
2. Service query database:
   SELECT * FROM pdf_documents ORDER BY created_at DESC
3. Return list
```

---

#### **POST /api/pdf/reprocess/{documentId}**

**Mục đích**: Xử lý lại PDF đã upload (Admin only)

**Authorization**: Admin role required

**Use cases**:
- PDF xử lý lỗi lần đầu
- Update embeddings sau khi đổi embedding model
- Re-index sau khi sửa processing logic

**Request**:
```
POST /api/pdf/reprocess/doc-001
Authorization: Bearer {admin-token}
```

**Response**:
```json
{
  "success": true,
  "message": "PDF reprocessing queued",
  "documentId": "doc-001"
}
```

**Workflow**:
```
1. Verify user có role = Admin
2. Check document exists
3. Delete old embeddings
4. Enqueue background task
5. Return 202 Accepted
```

---

### 3.4. Background Processing Details

**Queue Architecture**:
```
PdfController
    ↓ Enqueue
BackgroundTaskQueue (IBackgroundTaskQueue)
    ↓ Dequeue
QueuedHostedService (Background Worker)
    ↓ Execute
PdfProcessingService.ProcessAndStorePdfAsync()
```

**Benefits**:
- Non-blocking API response
- Client không phải đợi processing (có thể mất 10-30 giây)
- Fault-tolerant: Retry logic if fails
- Scalable: Multiple workers có thể xử lý parallel

**Service Scope Handling**:
```csharp
await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
{
    using var scope = _serviceProvider.CreateScope();
    var pdfService = scope.ServiceProvider.GetRequiredService<IPdfProcessingService>();
    await pdfService.ProcessAndStorePdfAsync(filePath, documentId);
});
```

---

### 3.5. PDF Text Extraction (iText 7)

**Code snippet**:
```csharp
public async Task<string> ExtractTextFromPdfAsync(string filePath)
{
    using var pdfReader = new PdfReader(filePath);
    using var pdfDoc = new PdfDocument(pdfReader);

    var text = new StringBuilder();
    for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
    {
        var strategy = new SimpleTextExtractionStrategy();
        string pageText = PdfTextExtractor.GetTextFromPage(
            pdfDoc.GetPage(page),
            strategy
        );
        text.AppendLine(pageText);
    }

    return text.ToString();
}
```

**Challenges Handled**:
- Scanned PDFs (no text layer) → OCR required (not implemented yet)
- Encrypted PDFs → Return error
- Corrupted PDFs → Try-catch và log error
- Large PDFs (1000+ pages) → Chunk processing

---

## 4. QueryController - Truy vấn thông minh

**File**: `FASSolution/src/FAS.Api/Controllers/QueryController.cs`

### 4.1. Tổng quan
Controller cung cấp intelligent query processing sử dụng FMS Agent Service.

### 4.2. Dependencies
```csharp
private readonly IQueryService _queryService;
private readonly ILogger<QueryController> _logger;
```

### 4.3. Endpoints

---

#### **POST /api/query**

**Mục đích**: Truy vấn thông minh với FMS Agent

**Authorization**: Requires JWT token

**Request**:
```json
{
  "query": "Tồn kho nguyên liệu thép hiện tại là bao nhiêu?",
  "userId": "user-123",
  "filters": {
    "dateFrom": "2025-10-01",
    "dateTo": "2025-10-31"
  }
}
```

**Workflow chi tiết**:

```mermaid
sequenceDiagram
    participant Client
    participant QueryController
    participant QueryService
    participant FMSAgentService
    participant SemanticKernel
    participant Database
    participant RagService
    participant OllamaService

    Client->>QueryController: POST /api/query
    QueryController->>QueryService: QueryAsync(request)

    QueryService->>FMSAgentService: ProcessQueryAsync(query, userId)

    Note over FMSAgentService: Step 1: Detect Query Type
    FMSAgentService->>FMSAgentService: AnalyzeQueryIntent(query)
    Note over FMSAgentService: Intent: "Inventory Query"

    Note over FMSAgentService: Step 2: Generate SQL
    FMSAgentService->>SemanticKernel: Generate SQL for intent
    SemanticKernel->>FMSAgentService: SQL query

    Note over FMSAgentService: Step 3: Execute SQL
    FMSAgentService->>Database: Execute generated SQL
    Database->>FMSAgentService: Result rows

    Note over FMSAgentService: Step 4: Get Context from RAG
    FMSAgentService->>RagService: RetrieveRelevantDocumentsAsync(query)
    RagService->>FMSAgentService: Related documents

    Note over FMSAgentService: Step 5: Assemble Context
    FMSAgentService->>FMSAgentService: CombineDataAndDocuments()
    Note over FMSAgentService: Context = SQL Results + Documents

    Note over FMSAgentService: Step 6: Generate Answer
    FMSAgentService->>OllamaService: ChatAsync(query + context)
    OllamaService->>FMSAgentService: AI answer

    FMSAgentService->>QueryService: AgentResponse

    Note over QueryService: Save to query_history
    QueryService->>Database: INSERT INTO query_history
    QueryService->>QueryController: QueryResult

    QueryController->>Client: 200 OK + QueryResult
```

**Response**:
```json
{
  "answer": "Hiện tại tồn kho thép là 1,500 kg tại Kho A và 2,300 kg tại Kho B, tổng cộng 3,800 kg.",
  "relatedDocuments": [
    "tbl_Inventory: 2 records",
    "tbl_GBMaterial: 1 record"
  ],
  "processingTimeMs": 3200,
  "queryType": "Inventory"
}
```

---

#### **GET /api/query/suggestions?q={partialQuery}**

**Mục đích**: Gợi ý câu hỏi dựa trên lịch sử

**Authorization**: Requires JWT token

**Request**:
```
GET /api/query/suggestions?q=tồn kho
```

**Workflow**:
```
1. Search query_history WHERE query LIKE '%tồn kho%'
2. Order by frequency và recency
3. Return top 10 similar queries
```

**Response**:
```json
{
  "suggestions": [
    "Tồn kho nguyên liệu thép hiện tại?",
    "Tồn kho sản phẩm A còn bao nhiêu?",
    "Kiểm tra tồn kho tháng 10"
  ]
}
```

---

#### **GET /api/query/examples**

**Mục đích**: Lấy câu hỏi mẫu

**Authorization**: Requires JWT token

**Response**:
```json
{
  "examples": [
    {
      "category": "Tồn kho",
      "questions": [
        "Hiện tại có bao nhiêu nguyên liệu trong kho?",
        "Nguyên liệu nào sắp hết hàng?",
        "Tồn kho thép không gỉ là bao nhiêu?"
      ]
    },
    {
      "category": "Đơn hàng",
      "questions": [
        "Tổng giá trị đơn hàng tháng này?",
        "Đơn hàng nào đang pending?",
        "Top 5 khách hàng mua nhiều nhất?"
      ]
    },
    {
      "category": "Sản xuất",
      "questions": [
        "Kế hoạch sản xuất tuần này?",
        "Lệnh sản xuất nào đang delay?",
        "Năng suất sản xuất tháng trước?"
      ]
    }
  ]
}
```

---

### 4.4. FMS Agent Service - Intelligent Processing

**Query Type Detection**:
```csharp
private string DetectQueryType(string query)
{
    if (query.Contains("tồn kho") || query.Contains("inventory"))
        return "Inventory";

    if (query.Contains("đơn hàng") || query.Contains("sales") || query.Contains("order"))
        return "SalesOrder";

    if (query.Contains("nguyên liệu") || query.Contains("material"))
        return "Material";

    if (query.Contains("sản xuất") || query.Contains("production"))
        return "Production";

    if (query.Contains("nhà cung cấp") || query.Contains("vendor"))
        return "Vendor";

    return "General";
}
```

**SQL Generation với Semantic Kernel**:
```csharp
var skPrompt = @"
Given the query: {{$query}}
Database schema: {{$schema}}

Generate a safe PostgreSQL SELECT query to answer the question.
Return ONLY the SQL query, no explanations.
";

var result = await _kernel.InvokePromptAsync(skPrompt, new()
{
    ["query"] = userQuery,
    ["schema"] = GetRelevantSchema(queryType)
});
```

**Safety Checks**:
- Chỉ cho phép SELECT queries
- Block DELETE, UPDATE, DROP, ALTER
- Parameterized để prevent SQL injection
- Timeout 10 giây cho query execution

---

## 5. SyncController - Đồng bộ Database

**File**: `FASSolution/src/FAS.Api/Controllers/SyncController.cs`

### 5.1. Tổng quan
Controller quản lý việc đồng bộ dữ liệu từ SQL Server (legacy) sang PostgreSQL.

### 5.2. Dependencies
```csharp
private readonly IDatabaseSyncService _syncService;
private readonly ILogger<SyncController> _logger;
```

### 5.3. Endpoints (All Admin only)

---

#### **POST /api/sync/all**

**Mục đích**: Đồng bộ tất cả bảng

**Authorization**: Admin role required

**Workflow**:

```mermaid
sequenceDiagram
    participant Admin
    participant SyncController
    participant SyncService
    participant SQLServer
    participant PostgreSQL

    Admin->>SyncController: POST /api/sync/all
    SyncController->>SyncController: Verify Admin role

    SyncController->>SyncService: SyncAllTablesAsync()

    loop For each table
        Note over SyncService: Tables: Materials, Products,<br/>Inventory, Sales, etc.

        SyncService->>SQLServer: SELECT * FROM tbl_{Table}
        SQLServer->>SyncService: Rows (batch 1000)

        loop For each batch
            SyncService->>SyncService: MapDataTypes()
            Note over SyncService: SQL Server types → PostgreSQL types

            SyncService->>PostgreSQL: INSERT ... ON CONFLICT DO UPDATE
            PostgreSQL->>SyncService: Success

            SyncService->>PostgreSQL: UPDATE sync_status
        end

        Note over SyncService: Log: "Synced {table}: {count} records"
    end

    SyncService->>SyncController: SyncResult
    SyncController->>Admin: 200 OK + result
```

**Response**:
```json
{
  "success": true,
  "message": "All tables synced successfully",
  "results": [
    {
      "tableName": "tbl_GBMaterial",
      "recordsSynced": 1250,
      "duration": "2.3s",
      "status": "Success"
    },
    {
      "tableName": "tbl_Inventory",
      "recordsSynced": 3400,
      "duration": "5.1s",
      "status": "Success"
    }
  ],
  "totalRecords": 15320,
  "totalDuration": "45.2s"
}
```

---

#### **POST /api/sync/table/{tableName}**

**Mục đích**: Đồng bộ một bảng cụ thể

**Authorization**: Admin only

**Request**:
```
POST /api/sync/table/tbl_Inventory
```

**Response**:
```json
{
  "success": true,
  "tableName": "tbl_Inventory",
  "recordsSynced": 3400,
  "lastSyncTime": "2025-11-03T14:30:00Z"
}
```

---

#### **GET /api/sync/status**

**Mục đích**: Xem trạng thái đồng bộ

**Authorization**: Admin only

**Response**:
```json
{
  "autoSyncEnabled": true,
  "syncIntervalMinutes": 30,
  "lastFullSync": "2025-11-03T14:00:00Z",
  "nextScheduledSync": "2025-11-03T14:30:00Z",
  "tables": [
    {
      "tableName": "tbl_Inventory",
      "lastSyncTime": "2025-11-03T14:00:00Z",
      "recordsSynced": 3400,
      "isSuccess": true
    },
    {
      "tableName": "tbl_SalesOrder",
      "lastSyncTime": "2025-11-03T14:00:15Z",
      "recordsSynced": 5600,
      "isSuccess": true
    }
  ]
}
```

---

### 5.4. Sync Logic Details

**Bảng được sync**:
```csharp
private readonly string[] _tablesToSync = new[]
{
    "tbl_GBMaterial",        // Nguyên liệu
    "tbl_Product",           // Sản phẩm
    "tbl_GBVendor",          // Nhà cung cấp
    "tbl_Customer",          // Khách hàng
    "tbl_Warehouse",         // Kho
    "tbl_GBXNKPO",           // Phiếu nhập
    "tbl_SalesOrder",        // Đơn hàng bán
    "tbl_SalesDelivery",     // Giao hàng
    "tbl_Inventory",         // Tồn kho
    "tbl_WorkOrder",         // Lệnh sản xuất
    "tbl_ProductionPlan"     // Kế hoạch sản xuất
};
```

**Type Mapping SQL Server → PostgreSQL**:
```csharp
private string MapDataType(string sqlServerType)
{
    return sqlServerType.ToLower() switch
    {
        "nvarchar" or "varchar" or "nchar" or "char" => "VARCHAR",
        "int" => "INTEGER",
        "bigint" => "BIGINT",
        "decimal" or "numeric" or "money" => "NUMERIC",
        "datetime" or "datetime2" or "smalldatetime" => "TIMESTAMP",
        "bit" => "BOOLEAN",
        "uniqueidentifier" => "UUID",
        "float" => "DOUBLE PRECISION",
        "text" or "ntext" => "TEXT",
        _ => "VARCHAR"
    };
}
```

**Upsert Strategy**:
```sql
INSERT INTO {targetTable} ({columns})
VALUES ({values})
ON CONFLICT ({primaryKey})
DO UPDATE SET
    column1 = EXCLUDED.column1,
    column2 = EXCLUDED.column2,
    ...
    updated_at = NOW();
```

---

### 5.5. Background Sync Service

**Configuration**:
```json
{
  "SyncSettings": {
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30,
    "BatchSize": 1000
  }
}
```

**Background Worker**:
```csharp
public class DatabaseSyncBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _syncService.SyncAllTablesAsync();
                _logger.LogInformation("Auto-sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
        }
    }
}
```

---

## 6. SystemController - Quản lý hệ thống

**File**: `FASSolution/src/FAS.Api/Controllers/SystemController.cs`

### 6.1. Tổng quan
Controller cung cấp health checks và system management endpoints.

### 6.2. Dependencies
```csharp
private readonly IOllamaService _ollamaService;
private readonly IDatabaseContext _databaseContext;
private readonly IVectorStoreService _vectorStoreService;
private readonly ILogger<SystemController> _logger;
```

### 6.3. Endpoints

---

#### **GET /api/system/health**

**Mục đích**: Health check cơ bản

**Authorization**: Public (không cần JWT)

**Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-03T14:30:00Z",
  "version": "1.0.0"
}
```

**Workflow**:
```
1. Return 200 OK nếu API đang chạy
2. Không check dependencies (fast check)
```

---

#### **GET /api/system/status**

**Mục đích**: Kiểm tra chi tiết tất cả services

**Authorization**: Requires JWT token

**Workflow**:

```mermaid
graph TD
    A[Client Request] --> B[SystemController]
    B --> C{Check SQL Server}
    B --> D{Check PostgreSQL}
    B --> E{Check Ollama}
    B --> F{Check Vector Store}

    C -->|Success| C1[✓ Connected]
    C -->|Failed| C2[✗ Connection Error]

    D -->|Success| D1[✓ Connected]
    D -->|Failed| D2[✗ Connection Error]

    E -->|Success| E1[✓ Model Available]
    E -->|Failed| E2[✗ Offline]

    F -->|Success| F1[✓ Ready, 1250 docs]
    F -->|Failed| F2[✗ Extension Missing]

    C1 --> G[Aggregate Status]
    C2 --> G
    D1 --> G
    D2 --> G
    E1 --> G
    E2 --> G
    F1 --> G
    F2 --> G

    G --> H[Return JSON Response]
```

**Response Success**:
```json
{
  "isHealthy": true,
  "timestamp": "2025-11-03T14:30:00Z",
  "services": {
    "sqlServer": {
      "status": "Connected",
      "responseTime": "45ms"
    },
    "postgreSQL": {
      "status": "Connected",
      "responseTime": "23ms"
    },
    "ollama": {
      "status": "Available",
      "chatModel": "gpt-oss:120b-cloud",
      "embeddingModel": "nomic-embed-text",
      "responseTime": "120ms"
    },
    "vectorStore": {
      "status": "Ready",
      "extension": "pgvector 0.3.2",
      "documentCount": 1250,
      "indexType": "ivfflat"
    }
  }
}
```

**Response with Errors**:
```json
{
  "isHealthy": false,
  "timestamp": "2025-11-03T14:30:00Z",
  "services": {
    "sqlServer": {
      "status": "Connected",
      "responseTime": "50ms"
    },
    "postgreSQL": {
      "status": "Error",
      "error": "Connection refused"
    },
    "ollama": {
      "status": "Unavailable",
      "error": "Connection timeout"
    },
    "vectorStore": {
      "status": "Error",
      "error": "Extension not installed"
    }
  },
  "errors": [
    "PostgreSQL connection failed",
    "Ollama service unavailable",
    "Vector store not initialized"
  ]
}
```

---

#### **POST /api/system/init-vector-store**

**Mục đích**: Khởi tạo pgvector extension và tables (one-time setup)

**Authorization**: Admin only

**Workflow**:
```
1. Verify Admin role
2. Check PostgreSQL connection
3. Execute SQL: CREATE EXTENSION IF NOT EXISTS vector;
4. Create document_embeddings table
5. Create IVFFLAT index
6. Return success
```

**Response**:
```json
{
  "success": true,
  "message": "Vector store initialized successfully",
  "details": {
    "extension": "pgvector 0.3.2",
    "tablesCreated": ["document_embeddings"],
    "indexesCreated": ["idx_embedding_ivfflat"]
  }
}
```

**SQL Executed**:
```sql
-- 1. Enable extension
CREATE EXTENSION IF NOT EXISTS vector;

-- 2. Create table
CREATE TABLE IF NOT EXISTS document_embeddings (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL UNIQUE,
    content TEXT NOT NULL,
    embedding vector(768),
    source VARCHAR(100),
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- 3. Create index
CREATE INDEX IF NOT EXISTS idx_embedding_ivfflat
ON document_embeddings
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

---

### 6.4. System Checks Implementation

**SQL Server Check**:
```csharp
private async Task<bool> CheckSqlServerAsync()
{
    try
    {
        using var connection = _databaseContext.CreateSqlServerConnection();
        await connection.OpenAsync();
        await connection.ExecuteScalarAsync("SELECT 1");
        return true;
    }
    catch
    {
        return false;
    }
}
```

**Ollama Check**:
```csharp
private async Task<bool> CheckOllamaAsync()
{
    return await _ollamaService.IsModelAvailableAsync();
}
```

**Vector Store Check**:
```csharp
private async Task<(bool success, int documentCount)> CheckVectorStoreAsync()
{
    try
    {
        var count = await _vectorStoreService.GetDocumentCountAsync();
        return (true, count);
    }
    catch
    {
        return (false, 0);
    }
}
```

---

## 7. Tổng kết Workflows

### 7.1. Request Flow Tổng quan

```
                        ┌─────────────┐
                        │   Client    │
                        └──────┬──────┘
                               │
                    ┌──────────▼──────────┐
                    │  JWT Middleware     │ (Validate token)
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │    Controller       │ (Route request)
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │  Business Service   │ (Logic)
                    └──────────┬──────────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
    ┌───────▼────────┐ ┌──────▼───────┐ ┌───────▼────────┐
    │  SQL Server    │ │  PostgreSQL  │ │   Ollama API   │
    └────────────────┘ └──────────────┘ └────────────────┘
```

### 7.2. Authentication Flow
```
Login → Validate Credentials → Generate JWT → Return Token
→ Subsequent requests include token → Middleware validates → Access granted/denied
```

### 7.3. RAG Query Flow
```
User Query → Generate Embedding → Vector Search → Retrieve Context
→ Augment Prompt → Call LLM → Generate Answer → Save History → Return Response
```

### 7.4. PDF Processing Flow
```
Upload PDF → Save to Disk → Queue Background Task → Return 202 Accepted
→ [Background] Extract Text → Generate Embeddings → Store in Vector DB → Update Status
```

### 7.5. Intelligent Query Flow
```
User Query → Detect Intent → Generate SQL → Execute Query → Get RAG Context
→ Combine Data → Generate Answer → Save History → Return Result
```

### 7.6. Database Sync Flow
```
Trigger Sync → For each table → Read from SQL Server (batch)
→ Map types → Upsert to PostgreSQL → Update sync status → Log completion
```

### 7.7. System Health Check Flow
```
Request → Check SQL Server → Check PostgreSQL → Check Ollama → Check Vector Store
→ Aggregate results → Return status + metrics
```

---

## 8. Performance Metrics

| Operation | Avg Time | Notes |
|-----------|----------|-------|
| JWT Generation | 5-10ms | Fast |
| JWT Validation | 3-5ms | Cached |
| Embedding Generation | 200-300ms | Depends on text length |
| Vector Search (Top 5) | 50-100ms | With IVFFLAT index |
| LLM Chat Response | 1000-3000ms | Depends on context size |
| Full RAG Query | 2000-4000ms | Total pipeline |
| PDF Text Extraction | 500ms/page | Varies by complexity |
| SQL Query Execution | 10-100ms | Depends on query |
| Database Sync (1000 rows) | 1-2s | Per batch |

---

## 9. Common Error Codes

| HTTP Code | Meaning | Common Causes |
|-----------|---------|---------------|
| 200 | Success | Request processed successfully |
| 202 | Accepted | Background task queued |
| 400 | Bad Request | Invalid input, validation failed |
| 401 | Unauthorized | Missing/invalid JWT token |
| 403 | Forbidden | Insufficient permissions (not Admin) |
| 404 | Not Found | Resource không tồn tại |
| 500 | Internal Server Error | Unhandled exception |
| 503 | Service Unavailable | External service (Ollama) offline |
| 504 | Gateway Timeout | LLM generation timeout |

---

## 10. Best Practices khi sử dụng API

### 10.1. Authentication
```javascript
// 1. Login first
const loginResponse = await fetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username: 'admin', password: 'xxx' })
});
const { token } = await loginResponse.json();

// 2. Use token in subsequent requests
const headers = {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
};
```

### 10.2. Chat với context
```javascript
// Maintain sessionId để AI nhớ context
const sessionId = generateUUID();

const response1 = await fetch('/api/chat', {
    method: 'POST',
    headers,
    body: JSON.stringify({
        message: "Tồn kho thép là bao nhiêu?",
        sessionId
    })
});

// AI sẽ nhớ context từ câu trước
const response2 = await fetch('/api/chat', {
    method: 'POST',
    headers,
    body: JSON.stringify({
        message: "Còn nhôm thì sao?",  // AI hiểu "còn ... thì sao" refers to tồn kho
        sessionId  // Same session
    })
});
```

### 10.3. PDF Upload
```javascript
const formData = new FormData();
formData.append('file', pdfFile);

const response = await fetch('/api/pdf/upload', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
});

const { documentId } = await response.json();

// Poll for processing status
const checkStatus = setInterval(async () => {
    const listResponse = await fetch('/api/pdf/list', { headers });
    const { documents } = await listResponse.json();
    const doc = documents.find(d => d.documentId === documentId);

    if (doc.processingStatus === 'Processed') {
        clearInterval(checkStatus);
        console.log('PDF ready for queries!');
    }
}, 3000);
```

### 10.4. Intelligent Query với filters
```javascript
const response = await fetch('/api/query', {
    method: 'POST',
    headers,
    body: JSON.stringify({
        query: "Top 5 sản phẩm bán chạy nhất?",
        filters: {
            dateFrom: "2025-10-01",
            dateTo: "2025-10-31",
            category: "Electronics"
        }
    })
});
```

---

**Cập nhật lần cuối**: 2025-11-03