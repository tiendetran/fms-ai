-- FMS RAG PostgreSQL Database Schema
-- Version: 1.0
-- Created: 2025-11-01

-- Enable pgvector extension for vector similarity search
CREATE EXTENSION IF NOT EXISTS vector;

-- Bảng Master Data
CREATE TABLE IF NOT EXISTS materials (
    id SERIAL PRIMARY KEY,
    material_code VARCHAR(50) NOT NULL UNIQUE,
    material_name VARCHAR(255) NOT NULL,
    description TEXT,
    unit_id INTEGER,
    standard_quantity DECIMAL(18,2),
    specifications TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    product_code VARCHAR(50) NOT NULL UNIQUE,
    product_name VARCHAR(255) NOT NULL,
    description TEXT,
    unit_id INTEGER,
    standard_price DECIMAL(18,2),
    specifications TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS units (
    id SERIAL PRIMARY KEY,
    unit_code VARCHAR(50) NOT NULL UNIQUE,
    unit_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS suppliers (
    id SERIAL PRIMARY KEY,
    supplier_code VARCHAR(50) NOT NULL UNIQUE,
    supplier_name VARCHAR(255) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    contact_person VARCHAR(100),
    tax_code VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS manufacturers (
    id SERIAL PRIMARY KEY,
    manufacturer_code VARCHAR(50) NOT NULL UNIQUE,
    manufacturer_name VARCHAR(255) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    contact_person VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS customers (
    id SERIAL PRIMARY KEY,
    customer_code VARCHAR(50) NOT NULL UNIQUE,
    customer_name VARCHAR(255) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    contact_person VARCHAR(100),
    tax_code VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS warehouses (
    id SERIAL PRIMARY KEY,
    warehouse_code VARCHAR(50) NOT NULL UNIQUE,
    warehouse_name VARCHAR(255) NOT NULL,
    location TEXT,
    warehouse_type VARCHAR(50),
    capacity DECIMAL(18,2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Phiếu Nhập Nguyên Liệu
CREATE TABLE IF NOT EXISTS material_receipts (
    id SERIAL PRIMARY KEY,
    receipt_code VARCHAR(50) NOT NULL UNIQUE,
    receipt_date TIMESTAMP NOT NULL,
    supplier_id INTEGER REFERENCES suppliers(id),
    warehouse_id INTEGER REFERENCES warehouses(id),
    invoice_number VARCHAR(100),
    total_amount DECIMAL(18,2),
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS material_receipt_details (
    id SERIAL PRIMARY KEY,
    receipt_id INTEGER REFERENCES material_receipts(id),
    material_id INTEGER REFERENCES materials(id),
    quantity DECIMAL(18,2),
    unit_price DECIMAL(18,2),
    total_price DECIMAL(18,2),
    batch_number VARCHAR(100),
    expiry_date TIMESTAMP,
    quality_status VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Phiếu Nhập Thành Phẩm
CREATE TABLE IF NOT EXISTS product_receipts (
    id SERIAL PRIMARY KEY,
    receipt_code VARCHAR(50) NOT NULL UNIQUE,
    receipt_date TIMESTAMP NOT NULL,
    warehouse_id INTEGER REFERENCES warehouses(id),
    production_order_id INTEGER,
    total_quantity DECIMAL(18,2),
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS product_receipt_details (
    id SERIAL PRIMARY KEY,
    receipt_id INTEGER REFERENCES product_receipts(id),
    product_id INTEGER REFERENCES products(id),
    quantity DECIMAL(18,2),
    batch_number VARCHAR(100),
    production_date TIMESTAMP,
    expiry_date TIMESTAMP,
    quality_status VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Đơn Hàng và Xuất Bán
CREATE TABLE IF NOT EXISTS sales_orders (
    id SERIAL PRIMARY KEY,
    order_code VARCHAR(50) NOT NULL UNIQUE,
    order_date TIMESTAMP NOT NULL,
    customer_id INTEGER REFERENCES customers(id),
    delivery_date TIMESTAMP,
    total_amount DECIMAL(18,2),
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS sales_order_details (
    id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES sales_orders(id),
    product_id INTEGER REFERENCES products(id),
    quantity DECIMAL(18,2),
    unit_price DECIMAL(18,2),
    total_price DECIMAL(18,2),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS sales_deliveries (
    id SERIAL PRIMARY KEY,
    delivery_code VARCHAR(50) NOT NULL UNIQUE,
    delivery_date TIMESTAMP NOT NULL,
    order_id INTEGER REFERENCES sales_orders(id),
    customer_id INTEGER REFERENCES customers(id),
    warehouse_id INTEGER REFERENCES warehouses(id),
    total_amount DECIMAL(18,2),
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS sales_delivery_details (
    id SERIAL PRIMARY KEY,
    delivery_id INTEGER REFERENCES sales_deliveries(id),
    product_id INTEGER REFERENCES products(id),
    quantity DECIMAL(18,2),
    unit_price DECIMAL(18,2),
    total_price DECIMAL(18,2),
    batch_number VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Tồn Kho
CREATE TABLE IF NOT EXISTS inventory (
    id SERIAL PRIMARY KEY,
    warehouse_id INTEGER REFERENCES warehouses(id),
    material_id INTEGER REFERENCES materials(id),
    product_id INTEGER REFERENCES products(id),
    quantity DECIMAL(18,2),
    reserved_quantity DECIMAL(18,2),
    available_quantity DECIMAL(18,2),
    batch_number VARCHAR(100),
    expiry_date TIMESTAMP,
    last_update_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Kế Hoạch và Lệnh Sản Xuất
CREATE TABLE IF NOT EXISTS production_plans (
    id SERIAL PRIMARY KEY,
    plan_code VARCHAR(50) NOT NULL UNIQUE,
    plan_date TIMESTAMP NOT NULL,
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS production_orders (
    id SERIAL PRIMARY KEY,
    order_code VARCHAR(50) NOT NULL UNIQUE,
    plan_id INTEGER REFERENCES production_plans(id),
    product_id INTEGER REFERENCES products(id),
    planned_quantity DECIMAL(18,2),
    actual_quantity DECIMAL(18,2),
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    status VARCHAR(50),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Document Embeddings cho RAG
CREATE TABLE IF NOT EXISTS document_embeddings (
    id SERIAL PRIMARY KEY,
    document_name VARCHAR(500) NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    source_table VARCHAR(100),
    source_record_id INTEGER,
    embedding vector(768), -- nomic-embed-text dimension
    metadata JSONB,
    indexed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Bảng Chat History
CREATE TABLE IF NOT EXISTS chat_history (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(100) NOT NULL,
    user_id VARCHAR(100) NOT NULL,
    user_message TEXT NOT NULL,
    assistant_response TEXT NOT NULL,
    context JSONB,
    tokens_used INTEGER,
    chat_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Create Indexes
CREATE INDEX idx_materials_code ON materials(material_code);
CREATE INDEX idx_products_code ON products(product_code);
CREATE INDEX idx_suppliers_code ON suppliers(supplier_code);
CREATE INDEX idx_customers_code ON customers(customer_code);
CREATE INDEX idx_material_receipts_date ON material_receipts(receipt_date);
CREATE INDEX idx_sales_orders_date ON sales_orders(order_date);
CREATE INDEX idx_inventory_warehouse ON inventory(warehouse_id);
CREATE INDEX idx_document_embeddings_type ON document_embeddings(document_type);
CREATE INDEX idx_document_embeddings_source ON document_embeddings(source_table, source_record_id);
CREATE INDEX idx_chat_history_session ON chat_history(session_id);
CREATE INDEX idx_chat_history_user ON chat_history(user_id);

-- Create vector index for similarity search
CREATE INDEX idx_document_embeddings_vector ON document_embeddings USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- Users table for authentication
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    full_name VARCHAR(255),
    email VARCHAR(100),
    role VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    last_login TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Insert default admin user (password: Admin@123)
INSERT INTO users (username, password_hash, full_name, role, is_active)
VALUES ('admin', '$2a$11$qqqqqqqqqqqqqqqqqqqqqeJ8cKj9nKO7lWQjJKhQ6K7VHGWyKKK', 'Administrator', 'Admin', TRUE)
ON CONFLICT (username) DO NOTHING;
