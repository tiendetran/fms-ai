-- Initialize FMS RAG Database

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255),
    role VARCHAR(50) DEFAULT 'User',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create index on username
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Insert default admin user (password: admin123)
-- NOTE: Change this password in production!
INSERT INTO users (username, password_hash, full_name, role)
VALUES ('admin', 'admin123', 'System Administrator', 'Admin')
ON CONFLICT (username) DO NOTHING;

-- Insert test user (password: user123)
INSERT INTO users (username, password_hash, full_name, role)
VALUES ('testuser', 'user123', 'Test User', 'User')
ON CONFLICT (username) DO NOTHING;

-- Create document_embeddings table
CREATE TABLE IF NOT EXISTS document_embeddings (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) UNIQUE NOT NULL,
    content TEXT NOT NULL,
    source VARCHAR(500),
    metadata JSONB,
    embedding vector(768),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for document_embeddings
CREATE INDEX IF NOT EXISTS idx_document_embeddings_vector 
ON document_embeddings USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

CREATE INDEX IF NOT EXISTS idx_document_embeddings_document_id 
ON document_embeddings(document_id);

CREATE INDEX IF NOT EXISTS idx_document_embeddings_source 
ON document_embeddings(source);

-- Create pdf_documents table
CREATE TABLE IF NOT EXISTS pdf_documents (
    id SERIAL PRIMARY KEY,
    document_id VARCHAR(255) UNIQUE NOT NULL,
    file_path VARCHAR(500),
    file_name VARCHAR(255),
    page_count INTEGER,
    processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create index on document_id
CREATE INDEX IF NOT EXISTS idx_pdf_documents_document_id ON pdf_documents(document_id);

-- Create sync_status table
CREATE TABLE IF NOT EXISTS sync_status (
    table_name VARCHAR(255) PRIMARY KEY,
    last_sync_time TIMESTAMP,
    row_count INTEGER,
    status VARCHAR(50)
);

-- Create query_history table
CREATE TABLE IF NOT EXISTS query_history (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(100),
    query TEXT NOT NULL,
    answer TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create index on query_history
CREATE INDEX IF NOT EXISTS idx_query_history_user_id ON query_history(user_id);
CREATE INDEX IF NOT EXISTS idx_query_history_created_at ON query_history(created_at DESC);

-- Grant permissions (adjust as needed)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'FMS RAG Database initialized successfully!';
    RAISE NOTICE 'Default users created:';
    RAISE NOTICE '  - Admin: username=admin, password=admin123';
    RAISE NOTICE '  - User: username=testuser, password=user123';
    RAISE NOTICE 'IMPORTANT: Change these passwords in production!';
END $$;
