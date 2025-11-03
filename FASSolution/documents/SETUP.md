# FMS RAG System - Setup Guide

Hướng dẫn chi tiết để setup và chạy FMS RAG System.

## Bước 1: Cài đặt Prerequisites

### 1.1. .NET 9.0 SDK
```bash
# Windows
winget install Microsoft.DotNet.SDK.9

# macOS
brew install dotnet@9

# Linux (Ubuntu/Debian)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

Kiểm tra:
```bash
dotnet --version
# Should show 9.0.x
```

### 1.2. PostgreSQL 18 with pgvector

#### Windows
1. Download PostgreSQL 18 từ https://www.postgresql.org/download/windows/
2. Cài đặt pgvector:
```bash
# Clone pgvector
git clone https://github.com/pgvector/pgvector.git
cd pgvector

# Build và install
make
make install
```

#### Linux (Ubuntu/Debian)
```bash
# Add PostgreSQL repo
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt-get update

# Install PostgreSQL 18
sudo apt-get install postgresql-18 postgresql-contrib-18

# Install pgvector
sudo apt-get install postgresql-18-pgvector
```

#### macOS
```bash
# Install PostgreSQL
brew install postgresql@18

# Install pgvector
brew install pgvector
```

#### Sử dụng Docker (Khuyến nghị - Dễ nhất)
```bash
docker run -d \
  --name fms-postgres \
  -e POSTGRES_DB=fms_rag \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=your_password \
  -p 5432:5432 \
  -v postgres_data:/var/lib/postgresql/data \
  pgvector/pgvector:pg18
```

### 1.3. Ollama

#### Windows
Download và cài đặt từ: https://ollama.ai/download/windows

#### macOS
```bash
brew install ollama
```

#### Linux
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

#### Pull required models
```bash
ollama pull gpt-oss:120b-cloud
ollama pull nomic-embed-text
```

Kiểm tra:
```bash
ollama list
# Should show both models
```

### 1.4. SQL Server (Source Database)
Đảm bảo SQL Server đã có database FMS với cấu trúc hiện tại.

## Bước 2: Setup Database

### 2.1. Khởi tạo PostgreSQL Database

#### Sử dụng script
```bash
psql -U postgres -f init-db.sql
```

#### Hoặc thủ công
```sql
-- Connect to PostgreSQL
psql -U postgres

-- Create database
CREATE DATABASE fms_rag;

-- Connect to database
\c fms_rag

-- Run init script
\i init-db.sql
```

### 2.2. Verify Setup
```sql
-- Check pgvector extension
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Check tables
\dt

-- Check users
SELECT * FROM users;
```

## Bước 3: Configure Application

### 3.1. Clone Project
```bash
git clone <repository-url>
cd FMS-RAG-System
```

### 3.2. Update appsettings.json
```bash
cd API
cp appsettings.json appsettings.Development.json
```

Chỉnh sửa `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR_SQL_SERVER;Database=FMS;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "PostgreSQL": "Host=localhost;Port=5432;Database=fms_rag;Username=postgres;Password=YOUR_PASSWORD;"
  },
  "OllamaSettings": {
    "Endpoint": "http://192.168.2.252:11434"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_THIS_TO_A_STRONG_SECRET_KEY_AT_LEAST_32_CHARS"
  }
}
```

**⚠️ QUAN TRỌNG:** 
- Thay đổi `SecretKey` thành một chuỗi bí mật mạnh
- Cập nhật connection strings với thông tin thực tế
- Không commit file này vào Git

### 3.3. Restore Dependencies
```bash
cd API
dotnet restore
```

## Bước 4: Build và Run

### 4.1. Build Project
```bash
dotnet build
```

### 4.2. Run Application
```bash
dotnet run
```

Hoặc sử dụng Visual Studio 2022:
- Open `FMS-RAG-System.sln`
- Set `API` project as startup project
- Press F5 to run

Application sẽ chạy tại:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## Bước 5: Initial Setup

### 5.1. Test API Health
```bash
curl http://localhost:5000/api/system/health
```

### 5.2. Login và Get Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

Lưu token nhận được để sử dụng cho các request tiếp theo.

### 5.3. Initialize Vector Store
```bash
curl -X POST http://localhost:5000/api/system/init-vector-store \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5.4. Sync Database
```bash
curl -X POST http://localhost:5000/api/sync/all \
  -H "Authorization: Bearer YOUR_TOKEN"
```

⏰ **Lưu ý:** Bước này có thể mất vài phút tùy thuộc vào kích thước database.

## Bước 6: Test System

### 6.1. Check System Status
```bash
curl http://localhost:5000/api/system/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 6.2. Test Query
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "query": "Cho tôi biết tồn kho nguyên liệu hiện tại"
  }'
```

### 6.3. Upload Test PDF
```bash
curl -X POST http://localhost:5000/api/pdf/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test-document.pdf"
```

### 6.4. Access Swagger UI
Mở browser và truy cập: `http://localhost:5000`

## Bước 7: Production Deployment

### 7.1. Sử dụng Docker Compose (Khuyến nghị)

```bash
# Update docker-compose.yml với thông tin production
vim docker-compose.yml

# Build và start services
docker-compose up -d

# Check logs
docker-compose logs -f api
```

### 7.2. Deploy lên IIS (Windows)

1. Publish project:
```bash
dotnet publish -c Release -o ./publish
```

2. Copy folder `publish` to IIS website folder

3. Configure IIS:
   - Create Application Pool (.NET CLR Version: No Managed Code)
   - Create Website pointing to publish folder
   - Set Application Pool for website

4. Update `appsettings.Production.json` với production settings

### 7.3. Deploy lên Linux Server

```bash
# Publish
dotnet publish -c Release -o /var/www/fms-rag

# Create systemd service
sudo nano /etc/systemd/system/fms-rag.service
```

Content của service file:
```ini
[Unit]
Description=FMS RAG API

[Service]
WorkingDirectory=/var/www/fms-rag
ExecStart=/usr/bin/dotnet /var/www/fms-rag/FMS.RAG.API.dll
Restart=always
RestartSec=10
SyslogIdentifier=fms-rag
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable và start service:
```bash
sudo systemctl enable fms-rag
sudo systemctl start fms-rag
sudo systemctl status fms-rag
```

## Troubleshooting

### Lỗi: "Ollama connection refused"
**Giải pháp:**
1. Kiểm tra Ollama service đang chạy: `ollama serve`
2. Test endpoint: `curl http://192.168.2.252:11434/api/tags`
3. Verify endpoint trong appsettings.json

### Lỗi: "PostgreSQL connection failed"
**Giải pháp:**
1. Kiểm tra PostgreSQL đang chạy: `sudo systemctl status postgresql`
2. Test connection: `psql -U postgres -d fms_rag`
3. Verify connection string trong appsettings.json

### Lỗi: "pgvector extension not found"
**Giải pháp:**
1. Cài đặt pgvector: `sudo apt-get install postgresql-18-pgvector`
2. Enable extension: `CREATE EXTENSION vector;`
3. Restart API

### Lỗi: "SQL Server connection failed"
**Giải pháp:**
1. Kiểm tra SQL Server đang chạy
2. Test connection string
3. Verify credentials và firewall settings

### Sync chậm hoặc timeout
**Giải pháp:**
1. Tăng timeout trong Dapper
2. Giảm `BatchSize` trong SyncSettings
3. Disable `AutoSyncEnabled` và sync thủ công
4. Sync từng bảng thay vì sync all

## Security Checklist

- [ ] Đổi default passwords (admin/admin123)
- [ ] Thay đổi JWT SecretKey
- [ ] Enable HTTPS trong production
- [ ] Implement password hashing (BCrypt)
- [ ] Configure firewall rules
- [ ] Setup SSL certificates
- [ ] Enable audit logging
- [ ] Regular backup database

## Next Steps

1. ✅ Setup monitoring và alerting
2. ✅ Configure backup strategy
3. ✅ Setup CI/CD pipeline
4. ✅ Implement comprehensive testing
5. ✅ Document API endpoints
6. ✅ Train users on system usage

## Support

Nếu gặp vấn đề:
1. Check logs in `Logs/` folder
2. Review Swagger documentation
3. Contact development team

---

**Chúc bạn setup thành công! 🎉**
