# FMS RAG System - Architecture

## Tổng quan Kiến trúc

FMS RAG System là một hệ thống phức tạp kết hợp nhiều công nghệ để tạo ra một giải pháp AI-powered quản lý nhà máy thông minh.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                       │
│          (Web UI, Mobile App, Desktop, API Clients)             │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTPS/JWT
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      FMS RAG API Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │ Controllers  │  │  Middleware  │  │  Authentication     │  │
│  │ - Query      │  │ - Error      │  │  - JWT Validation   │  │
│  │ - Sync       │  │   Handling   │  │  - Authorization    │  │
│  │ - PDF        │  │ - Logging    │  │                     │  │
│  │ - Auth       │  │              │  │                     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Service Layer                              │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │ FMS Agent    │  │ Query        │  │  Vector Store       │  │
│  │ Service      │  │ Service      │  │  Service            │  │
│  │              │  │              │  │                     │  │
│  │ - Semantic   │  │ - Query      │  │  - Embeddings       │  │
│  │   Kernel     │  │   Processing │  │  - Similarity       │  │
│  │ - Context    │  │ - History    │  │    Search           │  │
│  │   Building   │  │              │  │                     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │ Database     │  │ PDF          │  │  Ollama             │  │
│  │ Sync Service │  │ Processing   │  │  Service            │  │
│  │              │  │ Service      │  │                     │  │
│  │ - SQL→PG     │  │ - Extract    │  │  - Chat Model       │  │
│  │ - Scheduler  │  │ - Chunk      │  │  - Embeddings       │  │
│  │ - Batch      │  │ - Index      │  │                     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Data Layer                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │ SQL Server   │  │ PostgreSQL   │  │  Ollama Server      │  │
│  │ (Source)     │  │ (Target)     │  │  (External)         │  │
│  │              │  │              │  │                     │  │
│  │ - FMS DB     │  │ - Synced     │  │  - gpt-oss:120b     │  │
│  │ - Legacy     │  │   Tables     │  │  - nomic-embed      │  │
│  │   Tables     │  │ - Vectors    │  │                     │  │
│  │              │  │   (pgvector) │  │                     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Components Chi tiết

### 1. API Layer

#### Controllers
- **AuthController**: Xác thực JWT
- **QueryController**: Xử lý truy vấn RAG
- **SyncController**: Quản lý đồng bộ database
- **PdfController**: Upload và quản lý PDF
- **SystemController**: Health check và system status

#### Middleware
- **ErrorHandlingMiddleware**: Xử lý lỗi toàn cục
- **Logging**: Serilog cho structured logging

### 2. Service Layer

#### FMSAgentService
Trái tim của hệ thống AI.

**Workflow:**
```
User Query
    ↓
1. Vector Search (tìm context liên quan)
    ↓
2. Query Type Detection (phân loại query)
    ↓
3. Database Query (nếu cần)
    ↓
4. Context Building (tổng hợp context)
    ↓
5. LLM Processing (Semantic Kernel + Ollama)
    ↓
6. Response Generation
    ↓
User Answer
```

**Công nghệ:**
- Microsoft Semantic Kernel
- Ollama Chat Completion
- Dynamic prompt engineering

#### VectorStoreService
Quản lý vector embeddings cho RAG.

**Features:**
- pgvector integration
- HNSW indexing cho fast similarity search
- Batch embedding generation
- Metadata filtering

**Process:**
```
Document/Text
    ↓
1. Text Preprocessing (clean, normalize)
    ↓
2. Chunking (split into manageable pieces)
    ↓
3. Embedding Generation (Ollama)
    ↓
4. Vector Storage (PostgreSQL + pgvector)
    ↓
5. Index Creation (IVFFLAT/HNSW)
```

#### DatabaseSyncService
Đồng bộ từ SQL Server sang PostgreSQL.

**Sync Strategy:**
```
SQL Server Tables
    ↓
1. Schema Discovery
    ↓
2. Type Mapping (SQL Server → PostgreSQL)
    ↓
3. Table Creation (if not exists)
    ↓
4. Batch Reading (configurable size)
    ↓
5. Upsert to PostgreSQL
    ↓
6. Status Tracking
```

**Tables Synced:**
- Master data (Materials, Products, Vendors, Customers)
- Transactions (PO, Sales, Deliveries)
- Production (Work Orders, Plans)
- Quality (Lab tests, QC results)
- Inventory

#### PdfProcessingService
Xử lý và index PDF documents.

**Pipeline:**
```
PDF Upload
    ↓
1. File Validation
    ↓
2. Text Extraction (iTextSharp)
    ↓
3. Content Chunking
    ↓
4. Embedding Generation
    ↓
5. Vector Storage
    ↓
6. Metadata Tracking
```

#### OllamaService
Interface với Ollama API.

**Capabilities:**
- Chat completion
- Embedding generation
- Model management
- Error handling & retry

### 3. Data Layer

#### SQL Server (Source)
- Legacy FMS database
- Read-only access
- ~50+ tables
- Complex schema

#### PostgreSQL (Target)
**Tables:**

**Synced Tables (từ SQL Server):**
- `tbl_gbmaterial` - Nguyên liệu
- `tbl_product` - Thành phẩm
- `tbl_gbvendor` - Nhà cung cấp
- `tbl_customer` - Khách hàng
- `tbl_gbxnkpo` - Phiếu nhập
- `tbl_salesorder` - Đơn hàng
- `tbl_salesdelivery` - Xuất hàng
- ... etc

**RAG Tables (mới tạo):**
- `document_embeddings` - Vector store
- `pdf_documents` - PDF metadata
- `query_history` - Query logs
- `sync_status` - Sync tracking
- `users` - User accounts

**Extensions:**
- pgvector - Vector similarity search

#### Ollama Server
- External service
- Models: gpt-oss:120b-cloud, nomic-embed-text
- REST API integration

## Data Flow

### Query Flow (RAG)
```
1. User submits query
   ↓
2. JWT validation
   ↓
3. Query embedding generation (Ollama)
   ↓
4. Vector similarity search (pgvector)
   ↓ (Top-K similar documents)
5. Query type detection
   ↓
6. Database query (if needed)
   ↓ (Relevant data)
7. Context assembly
   ↓
8. Prompt construction
   ↓
9. LLM inference (Ollama)
   ↓
10. Response formatting
   ↓
11. Return to user + log history
```

### Sync Flow
```
1. Scheduled trigger / Manual trigger
   ↓
2. Connect to SQL Server
   ↓
3. For each table:
   a. Get schema
   b. Create/update PG table
   c. Batch read data
   d. Transform data types
   e. Upsert to PostgreSQL
   f. Update sync status
   ↓
4. Log completion
```

### PDF Processing Flow
```
1. PDF upload via API
   ↓
2. File validation & storage
   ↓
3. Background processing:
   a. Text extraction
   b. Content chunking
   c. Embedding generation
   d. Vector storage
   ↓
4. Update PDF registry
   ↓
5. Available for RAG queries
```

## Security Architecture

### Authentication & Authorization
```
Login Request
    ↓
1. Credentials validation (DB)
    ↓
2. JWT generation
    ↓
3. Token includes: UserID, Username, Role
    ↓
API Request with Token
    ↓
4. Token validation
    ↓
5. Role-based authorization
    ↓
6. Access granted/denied
```

**Security Layers:**
- HTTPS (TLS 1.2+)
- JWT Bearer tokens
- Role-based access control (RBAC)
- Password hashing (production: BCrypt)
- SQL injection prevention (Dapper parameterization)
- CORS configuration

## Scalability Considerations

### Horizontal Scaling
- Stateless API design
- Load balancer ready
- Database connection pooling

### Vertical Scaling
- Configurable batch sizes
- Memory-efficient streaming
- Async/await patterns

### Performance Optimizations
- Vector index (IVFFLAT)
- Database indexes
- Caching strategy (future)
- Background processing
- Batch operations

## Monitoring & Logging

### Logging Strategy
**Serilog Configuration:**
- Structured logging
- File sink (daily rolling)
- Log levels: Debug, Info, Warning, Error
- Context enrichment

**Log Categories:**
- HTTP requests/responses
- Database operations
- AI/ML operations
- Sync operations
- Errors & exceptions

### Monitoring Points
- API health endpoint
- System status endpoint
- Sync status tracking
- Query performance metrics
- Ollama availability

## Deployment Architecture

### Development
```
Developer Machine
    ↓
- Local PostgreSQL
- Local/Remote Ollama
- Local SQL Server (optional)
- dotnet run
```

### Production (Docker)
```
Docker Host
    ↓
┌─────────────────────┐
│  Docker Network     │
│  ┌───────────────┐  │
│  │ API Container │  │
│  └───────────────┘  │
│  ┌───────────────┐  │
│  │ PG Container  │  │
│  │ (pgvector)    │  │
│  └───────────────┘  │
└─────────────────────┘
    ↓ (external)
Ollama Server (separate)
SQL Server (existing)
```

### Production (IIS/Linux)
```
Load Balancer
    ↓
┌─────────────────────┐
│  API Server 1       │
│  (IIS/systemd)      │
└─────────────────────┘
┌─────────────────────┐
│  API Server 2       │
│  (IIS/systemd)      │
└─────────────────────┘
    ↓
┌─────────────────────┐
│  PostgreSQL         │
│  (Primary/Replica)  │
└─────────────────────┘
```

## Technology Stack Summary

| Layer | Technology | Purpose |
|-------|-----------|---------|
| API Framework | ASP.NET Core 9.0 | Web API |
| Authentication | JWT Bearer | Security |
| ORM | Dapper | Data access |
| Source DB | SQL Server | Legacy data |
| Target DB | PostgreSQL 18 | Modern data + vectors |
| Vector Store | pgvector | Similarity search |
| AI Framework | Microsoft Semantic Kernel | Agent orchestration |
| LLM | Ollama (gpt-oss:120b) | Chat completion |
| Embeddings | Ollama (nomic-embed-text) | Vector generation |
| PDF Processing | iTextSharp | Text extraction |
| Logging | Serilog | Structured logging |
| Documentation | Swagger/OpenAPI | API docs |
| Containerization | Docker | Deployment |

## Future Enhancements

### Phase 2
- [ ] Caching layer (Redis)
- [ ] Message queue (RabbitMQ/Kafka)
- [ ] Real-time notifications (SignalR)
- [ ] Advanced analytics dashboard

### Phase 3
- [ ] Multi-language support
- [ ] Voice interface
- [ ] Mobile apps (MAUI)
- [ ] Advanced reporting

### Phase 4
- [ ] Predictive analytics
- [ ] Automated decision making
- [ ] Integration với ERP systems
- [ ] Blockchain tracking

## Conclusion

FMS RAG System là một kiến trúc hiện đại, kết hợp best practices của:
- Clean Architecture
- Microservices principles
- AI/ML integration
- Enterprise security
- Scalable design

Hệ thống được thiết kế để dễ dàng mở rộng, bảo trì và tích hợp với các hệ thống khác.
