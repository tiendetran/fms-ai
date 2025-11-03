# Hướng dẫn Cài đặt Chi tiết - FMS RAG System

## 📋 Checklist Chuẩn bị

- [ ] Visual Studio 2022 (17.8 trở lên)
- [ ] .NET 9.0 SDK
- [ ] PostgreSQL 18
- [ ] SQL Server (database FMS hiện tại)
- [ ] Ollama server với 2 models: gpt-oss:120b-cloud và nomic-embed-text
- [ ] Git (optional)

## Bước 1: Cài đặt Prerequisites

### 1.1. Cài đặt .NET 9.0 SDK
```bash
# Download từ: https://dotnet.microsoft.com/download/dotnet/9.0
# Sau khi cài, kiểm tra:
dotnet --version
# Expected: 9.0.x
```

### 1.2. Cài đặt PostgreSQL 18
```bash
# Download từ: https://www.postgresql.org/download/
# Trong quá trình cài đặt, ghi nhớ:
# - Port: 5432 (default)
# - Username: postgres
# - Password: [your_password]
```

### 1.3. Cài đặt pgvector extension
```bash
# Windows: Download từ https://github.com/pgvector/pgvector/releases
# Hoặc sử dụng Stack Builder trong PostgreSQL installation

# Linux:
sudo apt install postgresql-18-pgvector

# Verify installation:
psql -U postgres -c "CREATE EXTENSION vector;"
```

### 1.4. Cài đặt Ollama
```bash
# Download từ: https://ollama.ai/download

# Pull models:
ollama pull gpt-oss:120b-cloud
ollama pull nomic-embed-text

# Verify:
ollama list
```

## Bước 2: Setup Database

### 2.1. Tạo PostgreSQL Database
```sql
-- Mở pgAdmin hoặc psql
-- Tạo database mới
CREATE DATABASE fms_rag
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Kết nối vào database
\c fms_rag

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify
SELECT * FROM pg_extension WHERE extname = 'vector';
```

### 2.2. Chạy Init Script
```bash
# Từ thư mục root của project
cd src/FMS.RAG.Infrastructure/Data

# Chạy script
psql -U postgres -d fms_rag -f init_postgres.sql

# Hoặc copy nội dung file và paste vào pgAdmin Query Tool
```

### 2.3. Verify Database Setup
```sql
-- Kiểm tra tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- Kiểm tra vector extension
SELECT * FROM document_embeddings LIMIT 1;
```

## Bước 3: Configure Project

### 3.1. Mở Project trong Visual Studio
```bash
# Double-click file
FMS_RAG_System.sln

# Hoặc từ VS: File > Open > Project/Solution
```

### 3.2. Cấu hình appsettings.json

Mở file: `src/FMS.RAG.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=YOUR_SQLSERVER_IP;Database=FMS;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;",
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=fms_rag;Username=postgres;Password=YOUR_POSTGRES_PASSWORD;"
  },
  "JwtSettings": {
    "SecretKey": "ThisIsAVerySecretKeyForJWTTokenGenerationPleaseChangeThis123!",
    "Issuer": "FMS_RAG_API",
    "Audience": "FMS_RAG_CLIENT",
    "ExpirationMinutes": 60
  },
  "OllamaSettings": {
    "Endpoint": "http://192.168.2.252:11434",
    "ChatModel": "gpt-oss:120b-cloud",
    "EmbeddingModel": "nomic-embed-text",
    "Temperature": 0.7,
    "MaxTokens": 2000
  },
  "SyncSettings": {
    "DatabaseSyncIntervalMinutes": 60,
    "PdfSyncIntervalMinutes": 30,
    "EnableAutoSync": true,
    "PdfFolder": "C:/FMS/Documents"
  }
}
```

**⚠️ Quan trọng:**
- Thay `YOUR_SQLSERVER_IP` bằng địa chỉ SQL Server thực tế
- Thay `YOUR_SQL_PASSWORD` và `YOUR_POSTGRES_PASSWORD`
- Thay `SecretKey` bằng key riêng của bạn (ít nhất 32 ký tự)
- Kiểm tra Ollama endpoint có đúng không

### 3.3. Tạo appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "FMS.RAG": "Debug"
    }
  },
  "ConnectionStrings": {
    "SqlServerConnection": "Server=localhost;Database=FMS;User Id=sa;Password=DevPassword123;TrustServerCertificate=True;",
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=fms_rag;Username=postgres;Password=postgres;"
  }
}
```

## Bước 4: Restore NuGet Packages

### Trong Visual Studio:
```
1. Right-click Solution trong Solution Explorer
2. Click "Restore NuGet Packages"
3. Đợi cho đến khi hoàn tất
```

### Hoặc Command Line:
```bash
cd FMS_RAG_System
dotnet restore
```

## Bước 5: Build Project

### Trong Visual Studio:
```
1. Build > Build Solution (Ctrl+Shift+B)
2. Kiểm tra Output window để đảm bảo không có lỗi
```

### Hoặc Command Line:
```bash
dotnet build
```

## Bước 6: Test Connection

### 6.1. Test Ollama Connection
```bash
# Test API
curl http://192.168.2.252:11434/api/version

# Test generation
curl http://192.168.2.252:11434/api/generate -d '{
  "model": "gpt-oss:120b-cloud",
  "prompt": "Hello",
  "stream": false
}'
```

### 6.2. Test PostgreSQL Connection
```bash
# Command line
psql -U postgres -d fms_rag -c "SELECT version();"

# Hoặc từ C#, chạy một test query
```

### 6.3. Test SQL Server Connection
```bash
# Sử dụng SSMS hoặc
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASSWORD -Q "SELECT @@VERSION"
```

## Bước 7: Run Application

### Trong Visual Studio:
```
1. Set FMS.RAG.Api làm Startup Project
2. Nhấn F5 để Run with Debug
3. Hoặc Ctrl+F5 để Run without Debug
```

### Hoặc Command Line:
```bash
cd src/FMS.RAG.Api
dotnet run
```

**Output Expected:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## Bước 8: Test API

### 8.1. Mở Swagger UI
```
Mở browser: https://localhost:7001/swagger
```

### 8.2. Test Login Endpoint
```http
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Response Expected:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "username": "admin",
    "expiresAt": "2025-11-02T10:00:00Z"
  }
}
```

### 8.3. Authorize trong Swagger
```
1. Click nút "Authorize" ở góc trên bên phải
2. Nhập: Bearer [YOUR_TOKEN]
3. Click "Authorize"
```

### 8.4. Test Sync Endpoint
```http
POST /api/sync/master-data
Authorization: Bearer YOUR_TOKEN
```

### 8.5. Test Chat Endpoint
```http
POST /api/chat
{
  "message": "Hello, xin chào!",
  "includeContext": true
}
```

## Bước 9: Initial Data Sync

### 9.1. Đồng bộ Master Data trước
```http
POST /api/sync/master-data
```

### 9.2. Đồng bộ Transaction Data
```http
POST /api/sync/material-receipts
POST /api/sync/product-receipts
POST /api/sync/sales-orders
POST /api/sync/inventory
```

### 9.3. Hoặc đồng bộ tất cả
```http
POST /api/sync/all
```

### 9.4. Index PDF Documents (nếu có)
```http
POST /api/sync/pdfs?folderPath=C:/FMS/Documents
```

## Bước 10: Monitoring & Logs

### 10.1. Check Logs
```
Location: FMS_RAG_System/src/FMS.RAG.Api/logs/
File: fms-rag-YYYY-MM-DD.txt
```

### 10.2. Monitor Background Service
```
Logs sẽ hiển thị:
- Database sync every 60 minutes
- PDF sync every 30 minutes
- Status của mỗi sync operation
```

## 🔧 Troubleshooting Common Issues

### Issue 1: Cannot connect to Ollama
```bash
# Check Ollama is running
curl http://192.168.2.252:11434/api/version

# If not running, start Ollama service
ollama serve
```

### Issue 2: pgvector extension not found
```sql
-- Install extension
CREATE EXTENSION vector;

-- If error, check pgvector is installed
SELECT * FROM pg_available_extensions WHERE name = 'vector';
```

### Issue 3: JWT token validation fails
```
Kiểm tra:
- SecretKey trong appsettings.json phải giống nhau cho tất cả environments
- SecretKey phải ít nhất 32 characters
- Token chưa expired
```

### Issue 4: Database sync fails
```
Kiểm tra:
- SQL Server connection string đúng
- Tables trong SQL Server có tồn tại không
- User có quyền đọc từ SQL Server
- PostgreSQL connection string đúng
```

### Issue 5: Embedding fails
```
Kiểm tra:
- Model nomic-embed-text đã được pull chưa: ollama list
- Ollama endpoint có accessible không
- Network có bị block không
```

## 📊 Verify Installation

Checklist cuối cùng:
- [ ] API runs without errors
- [ ] Swagger UI accessible
- [ ] Login successful
- [ ] Database sync works
- [ ] Chat endpoint returns response
- [ ] Background service running
- [ ] Logs being written

## 🎉 Hoàn tất!

Bạn đã cài đặt thành công FMS RAG System!

**Next Steps:**
1. Customize sync schedule trong appsettings.json
2. Add more data sources
3. Optimize vector search parameters
4. Setup production environment
5. Configure backup strategy

## 📞 Support

Nếu gặp vấn đề:
1. Check logs trong `logs/` folder
2. Review README.md
3. Contact: your.email@company.com
