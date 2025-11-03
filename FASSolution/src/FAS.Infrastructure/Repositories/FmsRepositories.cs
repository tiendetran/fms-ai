using Dapper;
using FAS.Core.Entities;
using FAS.Core.Interfaces;

namespace FAS.Infrastructure.Repositories;

public class MaterialReceiptRepository : BaseRepository<MaterialReceipt>, IMaterialReceiptRepository
{
    public MaterialReceiptRepository(IDbContext dbContext)
        : base(dbContext, "material_receipts")
    {
    }

    public async Task<IEnumerable<MaterialReceipt>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = @"
            SELECT * FROM material_receipts 
            WHERE receipt_date >= @FromDate AND receipt_date <= @ToDate 
            AND is_deleted = FALSE
            ORDER BY receipt_date DESC";
        return await connection.QueryAsync<MaterialReceipt>(sql, new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task<IEnumerable<MaterialReceipt>> GetBySupplierAsync(int supplierId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM material_receipts WHERE supplier_id = @SupplierId AND is_deleted = FALSE ORDER BY receipt_date DESC";
        return await connection.QueryAsync<MaterialReceipt>(sql, new { SupplierId = supplierId });
    }

    public async Task<MaterialReceipt?> GetByCodeAsync(string receiptCode)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM material_receipts WHERE receipt_code = @ReceiptCode AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<MaterialReceipt>(sql, new { ReceiptCode = receiptCode });
    }
}

public class ProductReceiptRepository : BaseRepository<ProductReceipt>, IProductReceiptRepository
{
    public ProductReceiptRepository(IDbContext dbContext)
        : base(dbContext, "product_receipts")
    {
    }

    public async Task<IEnumerable<ProductReceipt>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = @"
            SELECT * FROM product_receipts 
            WHERE receipt_date >= @FromDate AND receipt_date <= @ToDate 
            AND is_deleted = FALSE
            ORDER BY receipt_date DESC";
        return await connection.QueryAsync<ProductReceipt>(sql, new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task<ProductReceipt?> GetByCodeAsync(string receiptCode)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM product_receipts WHERE receipt_code = @ReceiptCode AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<ProductReceipt>(sql, new { ReceiptCode = receiptCode });
    }
}

public class SalesOrderRepository : BaseRepository<SalesOrder>, ISalesOrderRepository
{
    public SalesOrderRepository(IDbContext dbContext)
        : base(dbContext, "sales_orders")
    {
    }

    public async Task<IEnumerable<SalesOrder>> GetByCustomerAsync(int customerId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM sales_orders WHERE customer_id = @CustomerId AND is_deleted = FALSE ORDER BY order_date DESC";
        return await connection.QueryAsync<SalesOrder>(sql, new { CustomerId = customerId });
    }

    public async Task<IEnumerable<SalesOrder>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = @"
            SELECT * FROM sales_orders 
            WHERE order_date >= @FromDate AND order_date <= @ToDate 
            AND is_deleted = FALSE
            ORDER BY order_date DESC";
        return await connection.QueryAsync<SalesOrder>(sql, new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task<SalesOrder?> GetByCodeAsync(string orderCode)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM sales_orders WHERE order_code = @OrderCode AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<SalesOrder>(sql, new { OrderCode = orderCode });
    }
}

public class InventoryRepository : BaseRepository<Inventory>, IInventoryRepository
{
    public InventoryRepository(IDbContext dbContext)
        : base(dbContext, "inventory")
    {
    }

    public async Task<IEnumerable<Inventory>> GetByWarehouseAsync(int warehouseId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM inventory WHERE warehouse_id = @WarehouseId AND is_deleted = FALSE";
        return await connection.QueryAsync<Inventory>(sql, new { WarehouseId = warehouseId });
    }

    public async Task<Inventory?> GetByMaterialAsync(int warehouseId, int materialId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM inventory WHERE warehouse_id = @WarehouseId AND material_id = @MaterialId AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<Inventory>(sql, new { WarehouseId = warehouseId, MaterialId = materialId });
    }

    public async Task<Inventory?> GetByProductAsync(int warehouseId, int productId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM inventory WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<Inventory>(sql, new { WarehouseId = warehouseId, ProductId = productId });
    }

    public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync(decimal threshold)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM inventory WHERE available_quantity < @Threshold AND is_deleted = FALSE";
        return await connection.QueryAsync<Inventory>(sql, new { Threshold = threshold });
    }
}

public class SupplierRepository : BaseRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(IDbContext dbContext)
        : base(dbContext, "suppliers")
    {
    }

    public async Task<Supplier?> GetByCodeAsync(string supplierCode)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM suppliers WHERE supplier_code = @SupplierCode AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierCode = supplierCode });
    }
}

public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(IDbContext dbContext)
        : base(dbContext, "customers")
    {
    }

    public async Task<Customer?> GetByCodeAsync(string customerCode)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM customers WHERE customer_code = @CustomerCode AND is_deleted = FALSE";
        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerCode = customerCode });
    }
}

public class ChatHistoryRepository : BaseRepository<ChatHistoryModel>, IChatHistoryRepository
{
    public ChatHistoryRepository(IDbContext dbContext)
        : base(dbContext, "chat_history")
    {
    }

    public async Task<IEnumerable<ChatHistoryModel>> GetBySessionIdAsync(string sessionId)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = "SELECT * FROM chat_history WHERE session_id = @SessionId AND is_deleted = FALSE ORDER BY chat_time ASC";
        return await connection.QueryAsync<ChatHistoryModel>(sql, new { SessionId = sessionId });
    }

    public async Task<IEnumerable<ChatHistoryModel>> GetByUserIdAsync(string userId, int limit = 50)
    {
        using var connection = _dbContext.CreatePostgreSqlConnection();
        var sql = @"
            SELECT * FROM chat_history 
            WHERE user_id = @UserId AND is_deleted = FALSE 
            ORDER BY chat_time DESC 
            LIMIT @Limit";
        return await connection.QueryAsync<ChatHistoryModel>(sql, new { UserId = userId, Limit = limit });
    }
}
