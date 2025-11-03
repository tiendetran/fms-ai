using FAS.Core.Entities;

namespace FAS.Core.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

public interface IMaterialReceiptRepository : IBaseRepository<MaterialReceipt>
{
    Task<IEnumerable<MaterialReceipt>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<MaterialReceipt>> GetBySupplierAsync(int supplierId);
    Task<MaterialReceipt?> GetByCodeAsync(string receiptCode);
}

public interface IProductReceiptRepository : IBaseRepository<ProductReceipt>
{
    Task<IEnumerable<ProductReceipt>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<ProductReceipt?> GetByCodeAsync(string receiptCode);
}

public interface ISalesOrderRepository : IBaseRepository<SalesOrder>
{
    Task<IEnumerable<SalesOrder>> GetByCustomerAsync(int customerId);
    Task<IEnumerable<SalesOrder>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<SalesOrder?> GetByCodeAsync(string orderCode);
}

public interface IInventoryRepository : IBaseRepository<Inventory>
{
    Task<IEnumerable<Inventory>> GetByWarehouseAsync(int warehouseId);
    Task<Inventory?> GetByMaterialAsync(int warehouseId, int materialId);
    Task<Inventory?> GetByProductAsync(int warehouseId, int productId);
    Task<IEnumerable<Inventory>> GetLowStockItemsAsync(decimal threshold);
}

public interface ISupplierRepository : IBaseRepository<Supplier>
{
    Task<Supplier?> GetByCodeAsync(string supplierCode);
}

public interface ICustomerRepository : IBaseRepository<Customer>
{
    Task<Customer?> GetByCodeAsync(string customerCode);
}

public interface IDocumentEmbeddingRepository : IBaseRepository<DocumentEmbedding>
{
    Task<IEnumerable<DocumentEmbedding>> SearchSimilarAsync(float[] queryEmbedding, int topK = 5);
    Task<IEnumerable<DocumentEmbedding>> GetByDocumentTypeAsync(string documentType);
    Task<bool> DeleteBySourceAsync(string sourceTable, int sourceRecordId);
}

public interface IChatHistoryRepository : IBaseRepository<ChatHistoryModel>
{
    Task<IEnumerable<ChatHistoryModel>> GetBySessionIdAsync(string sessionId);
    Task<IEnumerable<ChatHistoryModel>> GetByUserIdAsync(string userId, int limit = 50);
}
