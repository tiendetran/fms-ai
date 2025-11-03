# FMS RAG System - Project Summary

## 📦 Tổng quan Project

**FMS RAG System** là một hệ thống API hoàn chỉnh được xây dựng với ASP.NET Core 9.0, tích hợp AI Agent và RAG (Retrieval-Augmented Generation) để cung cấp khả năng truy vấn và báo cáo thông minh cho hệ thống quản lý nhà máy (Factory Management System).

## 🎯 Mục tiêu

Tạo ra một hệ thống có khả năng:
1. ✅ Truy vấn thông minh với ngôn ngữ tự nhiên
2. ✅ Tự động đồng bộ dữ liệu từ SQL Server sang PostgreSQL
3. ✅ Xử lý và index PDF documents
4. ✅ Sử dụng AI Agent để phân tích và trả lời câu hỏi
5. ✅ Bảo mật với JWT Authentication
6. ✅ Scalable và maintainable architecture

## 📂 Cấu trúc Project

```
FMS-RAG-System/
├── API/
│   ├── Controllers/          # API Controllers
│   │   ├── AuthController.cs       # JWT Authentication
│   │   ├── QueryController.cs      # RAG Queries
│   │   ├── SyncController.cs       # Database Sync
│   │   ├── PdfController.cs        # PDF Management
│   │   └── SystemController.cs     # Health & Status
│   │
│   ├── Services/            # Business Logic
│   │   ├── AuthService.cs              # Authentication service
│   │   ├── FMSAgentService.cs          # AI Agent (Semantic Kernel)
│   │   ├── OllamaService.cs            # Ollama integration
│   │   ├── VectorStoreService.cs       # Vector embeddings (pgvector)
│   │   ├── DatabaseSyncService.cs      # SQL→PostgreSQL sync
│   │   ├── PdfProcessingService.cs     # PDF text extraction
│   │   ├── QueryService.cs             # Query processing
│   │   └── DatabaseSyncBackgroundService.cs  # Auto-sync scheduler
│   │
│   ├── Data/                # Data Access
│   │   └── DatabaseContext.cs          # Dapper connection factory
│   │
│   ├── Middleware/          # HTTP Middleware
│   │   └── ErrorHandlingMiddleware.cs  # Global error handling
│   │
│   ├── Program.cs           # Application entry point
│   ├── appsettings.json     # Configuration
│   ├── Dockerfile           # Docker image definition
│   └── FMS.RAG.API.csproj   # Project file
│
├── Documentation/
│   ├── README.md            # User guide
│   ├── SETUP.md            # Setup instructions
│   └── ARCHITECTURE.md     # System architecture
│
├── Database/
│   └── init-db.sql         # PostgreSQL initialization
│
├── Docker/
│   └── docker-compose.yml  # Multi-container setup
│
├── Scripts/
│   └── test-api.sh         # API testing script
│
└── .gitignore              # Git ignore rules
```

## 🔧 Công nghệ sử dụng

### Backend Framework
- **ASP.NET Core 9.0** - Modern web framework
- **C# 13** - Programming language

### Database
- **SQL Server** - Source database (existing FMS)
- **PostgreSQL 18** - Target database + Vector store
- **Dapper** - Lightweight ORM
- **pgvector** - Vector similarity search extension

### AI/ML
- **Ollama** - Local LLM inference
  - Chat Model: `gpt-oss:120b-cloud`
  - Embedding Model: `nomic-embed-text`
- **Microsoft Semantic Kernel** - Agent framework
- **RAG** - Retrieval-Augmented Generation pattern

### Security
- **JWT Bearer Tokens** - Authentication
- **Role-based Authorization** - Access control
- **HTTPS/TLS** - Transport security

### Tools & Libraries
- **Swagger/OpenAPI** - API documentation
- **Serilog** - Structured logging
- **iTextSharp** - PDF text extraction
- **OllamaSharp** - Ollama .NET client
- **Npgsql** - PostgreSQL driver

### DevOps
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration

## 📋 Tính năng chính

### 1. RAG System (Retrieval-Augmented Generation)
- Vector embeddings cho documents và text
- Similarity search với pgvector
- Context-aware responses
- Multi-source information retrieval

### 2. AI Agent (Microsoft Semantic Kernel)
- Intelligent query processing
- Query type detection
- Dynamic database queries
- Context building
- Natural language understanding

### 3. Database Synchronization
- Automated SQL Server → PostgreSQL sync
- Schema discovery và mapping
- Batch processing
- Incremental sync
- Status tracking
- Background scheduling (configurable intervals)

### 4. PDF Processing
- Upload PDF documents
- Automatic text extraction
- Content chunking
- Vector embedding generation
- Searchable PDF content
- Metadata tracking

### 5. Authentication & Authorization
- JWT-based authentication
- Role-based access control (User, Admin)
- Token expiration management
- Secure password handling (ready for BCrypt)

### 6. API Management
- RESTful API design
- Swagger UI for testing
- Comprehensive error handling
- Request/response logging
- CORS configuration

## 🚀 Quick Start

### Prerequisites
```bash
# Install .NET 9.0 SDK
# Install PostgreSQL 18 with pgvector
# Install Ollama and pull models:
ollama pull gpt-oss:120b-cloud
ollama pull nomic-embed-text
```

### Setup
```bash
# 1. Clone project
cd FMS-RAG-System

# 2. Setup PostgreSQL
psql -U postgres -f init-db.sql

# 3. Configure appsettings.json
# Update connection strings and settings

# 4. Run application
cd API
dotnet run
```

### Test
```bash
# Run test script
./test-api.sh

# Or access Swagger UI
http://localhost:5000
```

### Docker Deployment
```bash
docker-compose up -d
```

## 📊 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/validate` - Validate token

### Query (RAG)
- `POST /api/query` - Submit query
- `GET /api/query/suggestions` - Get query suggestions
- `GET /api/query/examples` - Get example queries

### Sync (Admin)
- `POST /api/sync/all` - Sync all tables
- `POST /api/sync/table/{name}` - Sync specific table
- `GET /api/sync/status` - Get sync status

### PDF
- `POST /api/pdf/upload` - Upload PDF
- `GET /api/pdf/list` - List all PDFs
- `POST /api/pdf/reprocess/{id}` - Reprocess PDF

### System
- `GET /api/system/health` - Health check
- `GET /api/system/status` - System status
- `POST /api/system/init-vector-store` - Initialize vectors

## 📈 Database Schema

### Synced Tables (từ SQL Server)
- Materials, Products, Vendors, Customers
- Purchase Orders, Sales Orders, Deliveries
- Production Plans, Work Orders
- Inventory, Quality Control data

### RAG Tables (mới)
- `document_embeddings` - Vector store (768 dimensions)
- `pdf_documents` - PDF metadata
- `query_history` - Query logs
- `sync_status` - Sync tracking
- `users` - User accounts

## 🔒 Security Features

- ✅ JWT Bearer Authentication
- ✅ Role-based Authorization
- ✅ Secure password storage (ready for BCrypt)
- ✅ SQL injection prevention (parameterized queries)
- ✅ HTTPS support
- ✅ CORS configuration
- ✅ Input validation
- ✅ Error message sanitization

## 📝 Configuration

### Key Settings (appsettings.json)
```json
{
  "ConnectionStrings": {
    "SqlServer": "...",
    "PostgreSQL": "..."
  },
  "OllamaSettings": {
    "Endpoint": "http://192.168.2.252:11434",
    "ChatModel": "gpt-oss:120b-cloud",
    "EmbeddingModel": "nomic-embed-text"
  },
  "SyncSettings": {
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30,
    "BatchSize": 1000
  },
  "VectorSettings": {
    "Dimensions": 768,
    "SimilarityThreshold": 0.7,
    "TopK": 5
  }
}
```

## 🧪 Testing

### Manual Testing
```bash
# 1. Health check
curl http://localhost:5000/api/system/health

# 2. Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# 3. Query
curl -X POST http://localhost:5000/api/query \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"query":"Tồn kho nguyên liệu hiện tại"}'
```

### Automated Testing
```bash
./test-api.sh
```

## 📚 Documentation

- **README.md** - User guide và overview
- **SETUP.md** - Chi tiết setup và installation
- **ARCHITECTURE.md** - System architecture và design
- **Swagger UI** - Interactive API documentation (http://localhost:5000)

## 🐛 Troubleshooting

### Common Issues

**Ollama Connection Failed**
```bash
# Check Ollama is running
curl http://192.168.2.252:11434/api/tags

# Verify models are pulled
ollama list
```

**Database Connection Failed**
```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Test connection
psql -U postgres -d fms_rag
```

**Vector Store Not Initialized**
```bash
# Initialize via API
curl -X POST http://localhost:5000/api/system/init-vector-store \
  -H "Authorization: Bearer TOKEN"
```

## 🔄 Development Workflow

### Local Development
1. Update code in Visual Studio 2022
2. Test locally with `dotnet run`
3. Use Swagger UI for API testing
4. Check logs in `Logs/` folder
5. Commit changes

### Deployment
1. Update version in csproj
2. Build Docker image
3. Test in staging environment
4. Deploy to production
5. Monitor logs and metrics

## 📦 Files Delivered

### Source Code (24 files)
- ✅ 5 Controllers
- ✅ 8 Services
- ✅ 1 Middleware
- ✅ 1 Database Context
- ✅ 1 Program.cs
- ✅ 1 Project file (.csproj)
- ✅ 1 Configuration (appsettings.json)

### Documentation (4 files)
- ✅ README.md
- ✅ SETUP.md
- ✅ ARCHITECTURE.md
- ✅ PROJECT_SUMMARY.md

### Deployment (4 files)
- ✅ Dockerfile
- ✅ docker-compose.yml
- ✅ init-db.sql
- ✅ .gitignore

### Tools (1 file)
- ✅ test-api.sh

**Total: 26 files**

## 🎯 Next Steps

### Immediate
1. ✅ Update connection strings in appsettings.json
2. ✅ Run init-db.sql to setup PostgreSQL
3. ✅ Start Ollama and pull models
4. ✅ Run the application
5. ✅ Test with test-api.sh

### Short-term
1. Implement BCrypt password hashing
2. Add comprehensive unit tests
3. Setup CI/CD pipeline
4. Configure production environment
5. Train users

### Long-term
1. Add caching layer (Redis)
2. Implement real-time notifications
3. Create web dashboard
4. Add advanced analytics
5. Mobile app integration

## 💡 Usage Examples

### Các câu query mẫu:
```
"Cho tôi biết danh sách nhập nguyên liệu trong tháng này"
"Tồn kho nguyên liệu hiện tại như thế nào?"
"Có bao nhiêu đơn hàng đang chờ sản xuất?"
"Thống kê xuất bán hàng tuần trước"
"Danh sách các nhà cung cấp nguyên liệu"
"Kế hoạch sản xuất trong tuần này"
"Báo cáo chất lượng sản phẩm tháng trước"
```

## 🎓 Learning Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Ollama Documentation](https://ollama.ai/docs)
- [pgvector Guide](https://github.com/pgvector/pgvector)
- [Dapper Tutorial](https://github.com/DapperLib/Dapper)

## 👥 Support & Contact

Để được hỗ trợ:
1. Kiểm tra documentation
2. Xem logs trong `Logs/` folder
3. Test với Swagger UI
4. Liên hệ development team

## 📄 License

Proprietary - Internal use only

---

**Developed with ❤️ for FMS Team**

*Version 1.0.0 - November 2025*
