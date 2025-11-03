# FMS RAG System - Factory Management System with RAG AI

Hệ thống quản lý nhà máy tích hợp AI Agent với RAG (Retrieval-Augmented Generation) để truy vấn và báo cáo thông minh.

## 🎯 Tính năng chính

### 1. RAG System (Retrieval-Augmented Generation)
- Truy vấn thông minh với vector embeddings
- Tìm kiếm ngữ nghĩa trong tài liệu và database
- Tích hợp Ollama với models:
  - Chat: `gpt-oss:120b-cloud`
  - Embedding: `nomic-embed-text`

### 2. Microsoft Agent Framework
- AI Agent tự động phân tích và trả lời câu hỏi
- Xử lý đa dạng loại truy vấn:
  - Nhập/xuất nguyên liệu
  - Quản lý tồn kho
  - Đơn hàng và sản xuất
  - Báo cáo và thống kê

### 3. Database Synchronization
- Tự động đồng bộ từ SQL Server sang PostgreSQL
- Sync theo lịch trình định kỳ
- Hỗ trợ sync thủ công theo bảng

### 4. PDF Processing
- Upload và xử lý PDF documents
- Tự động trích xuất text và tạo embeddings
- Tích hợp vào RAG system

### 5. JWT Authentication
- Bảo mật API với JSON Web Tokens
- Role-based access control

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 9.0 Web API
- **Database**: 
  - SQL Server (Source)
  - PostgreSQL 18 (Target + Vector Store)
- **ORM**: Dapper
- **AI/ML**:
  - Ollama (LLM & Embeddings)
  - Microsoft Semantic Kernel (Agent Framework)
  - pgvector (Vector similarity search)
- **Authentication**: JWT Bearer
- **Logging**: Serilog
- **Documentation**: Swagger/OpenAPI

## 📋 Requirements

### Software
- .NET 9.0 SDK
- PostgreSQL 18 with pgvector extension
- SQL Server (source database)
- Ollama server with required models

### Ollama Models
```bash
# Pull required models
ollama pull gpt-oss:120b-cloud
ollama pull nomic-embed-text
```

## 🚀 Installation

### 1. Clone repository
```bash
git clone <repository-url>
cd FMS-RAG-System
```

### 2. Configure appsettings.json
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR_SERVER;Database=FMS;User Id=sa;Password=YOUR_PASSWORD;",
    "PostgreSQL": "Host=localhost;Port=5432;Database=fms_rag;Username=postgres;Password=YOUR_PASSWORD;"
  },
  "OllamaSettings": {
    "Endpoint": "http://192.168.2.252:11434"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyHere"
  }
}
```

### 3. Setup PostgreSQL
```sql
-- Create database
CREATE DATABASE fms_rag;

-- Connect to database
\c fms_rag

-- Enable pgvector extension
CREATE EXTENSION vector;

-- Create initial user (for testing)
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255),
    role VARCHAR(50) DEFAULT 'User',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert test user (password: admin123)
INSERT INTO users (username, password_hash, full_name, role)
VALUES ('admin', 'admin123', 'Administrator', 'Admin');
```

### 4. Build and Run
```bash
cd API
dotnet restore
dotnet build
dotnet run
```

API sẽ chạy tại: `http://localhost:5000`

## 📖 API Documentation

Sau khi chạy ứng dụng, truy cập Swagger UI tại: `http://localhost:5000`

### Key Endpoints

#### Authentication
- `POST /api/auth/login` - Đăng nhập và lấy JWT token
- `POST /api/auth/validate` - Validate token

#### Query (RAG)
- `POST /api/query` - Truy vấn hệ thống với RAG
- `GET /api/query/suggestions?q=` - Lấy gợi ý câu hỏi
- `GET /api/query/examples` - Ví dụ câu hỏi

#### Sync
- `POST /api/sync/all` - Đồng bộ tất cả bảng (Admin only)
- `POST /api/sync/table/{tableName}` - Đồng bộ một bảng
- `GET /api/sync/status` - Trạng thái đồng bộ

#### PDF
- `POST /api/pdf/upload` - Upload PDF document
- `GET /api/pdf/list` - Danh sách PDFs
- `POST /api/pdf/reprocess/{documentId}` - Xử lý lại PDF

#### System
- `GET /api/system/health` - Health check
- `GET /api/system/status` - System status
- `POST /api/system/init-vector-store` - Khởi tạo vector store

## 💡 Usage Examples

### 1. Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

### 2. Query với RAG
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "query": "Cho tôi biết tồn kho nguyên liệu hiện tại"
  }'
```

### 3. Upload PDF
```bash
curl -X POST http://localhost:5000/api/pdf/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf"
```

## 🔧 Configuration

### Sync Settings
```json
{
  "SyncSettings": {
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30,
    "BatchSize": 1000
  }
}
```

### Vector Settings
```json
{
  "VectorSettings": {
    "Dimensions": 768,
    "SimilarityThreshold": 0.7,
    "TopK": 5
  }
}
```

## 📊 Database Schema

### Tables Synced from SQL Server
- `tbl_GBMaterial` - Nguyên liệu
- `tbl_Product` - Thành phẩm
- `tbl_GBVendor` - Nhà cung cấp
- `tbl_Customer` - Khách hàng
- `tbl_GBXNKPO` - Phiếu nhập
- `tbl_SalesOrder` - Đơn hàng
- `tbl_SalesDelivery` - Xuất hàng
- ... và nhiều bảng khác

### PostgreSQL Tables (Created by API)
- `document_embeddings` - Vector embeddings cho RAG
- `pdf_documents` - PDF document metadata
- `sync_status` - Trạng thái đồng bộ
- `query_history` - Lịch sử truy vấn
- `users` - User accounts

## 🧪 Testing

### Test với curl
```bash
# Health check
curl http://localhost:5000/api/system/health

# System status (requires auth)
curl http://localhost:5000/api/system/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Test queries
```
"Cho tôi biết danh sách nhập nguyên liệu trong tháng này"
"Tồn kho nguyên liệu hiện tại như thế nào?"
"Có bao nhiêu đơn hàng đang chờ sản xuất?"
"Thống kê xuất bán hàng tuần trước"
"Danh sách các nhà cung cấp nguyên liệu"
```

## 📝 Logging

Logs được lưu trong thư mục `Logs/` với format:
- File: `fms-rag-YYYY-MM-DD.txt`
- Rolling: Daily
- Format: `[Timestamp] [Level] Message`

## 🔐 Security

- JWT tokens expire sau 60 phút (có thể cấu hình)
- Role-based authorization cho Admin endpoints
- CORS được cấu hình cho development
- Passwords nên hash với BCrypt (hiện tại demo đơn giản)

## 🐛 Troubleshooting

### Ollama không kết nối được
- Kiểm tra Ollama service đang chạy
- Verify endpoint URL trong appsettings.json
- Test: `curl http://192.168.2.252:11434/api/tags`

### PostgreSQL connection error
- Kiểm tra PostgreSQL đang chạy
- Verify connection string
- Đảm bảo pgvector extension đã được cài đặt

### Vector store not initialized
- Run: `POST /api/system/init-vector-store`
- Kiểm tra logs để xem lỗi chi tiết

## 📚 References

- [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Ollama Documentation](https://ollama.ai/docs)
- [pgvector](https://github.com/pgvector/pgvector)
- [Dapper](https://github.com/DapperLib/Dapper)

## 👥 Support

Để được hỗ trợ, vui lòng:
1. Kiểm tra logs trong thư mục `Logs/`
2. Xem Swagger documentation
3. Liên hệ team phát triển

## 📄 License

Proprietary - Internal use only
