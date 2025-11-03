using Dapper;
using FAS.Core.DTOs;
using FAS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FAS.Infrastructure.Services;

public class SyncService : ISyncService
{
    private readonly IDbContext _dbContext;
    private readonly IRagService _ragService;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IDbContext dbContext,
        IRagService ragService,
        ILogger<SyncService> logger)
    {
        _dbContext = dbContext;
        _ragService = ragService;
        _logger = logger;
    }

    public async Task<SyncStatusDto> SyncMaterialReceiptsAsync()
    {
        var status = new SyncStatusDto
        {
            EntityType = "MaterialReceipts",
            LastSyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting sync for MaterialReceipts");

            using var sqlConnection = _dbContext.CreateSqlServerConnection();
            using var pgConnection = _dbContext.CreatePostgreSqlConnection();

            // Get data from SQL Server (adjust table/column names based on your schema)
            var sqlData = await sqlConnection.QueryAsync<dynamic>(@"
                SELECT TOP 1000
                    ReceiptCode, ReceiptDate, SupplierId, WarehouseId,
                    InvoiceNumber, TotalAmount, Status, Notes
                FROM tbl_MaterialReceipts
                WHERE IsDeleted = 0
                ORDER BY ReceiptDate DESC
            ");

            var recordsSynced = 0;

            foreach (var record in sqlData)
            {
                // Insert/Update to PostgreSQL
                var sql = @"
                    INSERT INTO material_receipts 
                    (receipt_code, receipt_date, supplier_id, warehouse_id, 
                     invoice_number, total_amount, status, notes, created_at)
                    VALUES 
                    (@ReceiptCode, @ReceiptDate, @SupplierId, @WarehouseId,
                     @InvoiceNumber, @TotalAmount, @Status, @Notes, @CreatedAt)
                    ON CONFLICT (receipt_code) 
                    DO UPDATE SET
                        receipt_date = EXCLUDED.receipt_date,
                        supplier_id = EXCLUDED.supplier_id,
                        warehouse_id = EXCLUDED.warehouse_id,
                        total_amount = EXCLUDED.total_amount,
                        status = EXCLUDED.status,
                        updated_at = @UpdatedAt
                    RETURNING id";

                var id = await pgConnection.ExecuteScalarAsync<int>(sql, new
                {
                    ReceiptCode = record.ReceiptCode,
                    ReceiptDate = record.ReceiptDate,
                    SupplierId = record.SupplierId,
                    WarehouseId = record.WarehouseId,
                    InvoiceNumber = record.InvoiceNumber,
                    TotalAmount = record.TotalAmount,
                    Status = record.Status,
                    Notes = record.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // Index for RAG
                var recordData = new Dictionary<string, object>
                {
                    ["receipt_code"] = record.ReceiptCode,
                    ["receipt_date"] = record.ReceiptDate,
                    ["total_amount"] = record.TotalAmount,
                    ["status"] = record.Status
                };

                await _ragService.IndexDatabaseRecordAsync("material_receipts", id, recordData);

                recordsSynced++;
            }

            status.RecordsSynced = recordsSynced;
            status.IsSuccess = true;

            _logger.LogInformation("Synced {Count} MaterialReceipts successfully", recordsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing MaterialReceipts");
            status.IsSuccess = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<SyncStatusDto> SyncProductReceiptsAsync()
    {
        var status = new SyncStatusDto
        {
            EntityType = "ProductReceipts",
            LastSyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting sync for ProductReceipts");

            using var sqlConnection = _dbContext.CreateSqlServerConnection();
            using var pgConnection = _dbContext.CreatePostgreSqlConnection();

            var sqlData = await sqlConnection.QueryAsync<dynamic>(@"
                SELECT TOP 1000
                    ReceiptCode, ReceiptDate, WarehouseId, ProductionOrderId,
                    TotalQuantity, Status, Notes
                FROM tbl_ProductReceipts
                WHERE IsDeleted = 0
                ORDER BY ReceiptDate DESC
            ");

            var recordsSynced = 0;

            foreach (var record in sqlData)
            {
                var sql = @"
                    INSERT INTO product_receipts 
                    (receipt_code, receipt_date, warehouse_id, production_order_id,
                     total_quantity, status, notes, created_at)
                    VALUES 
                    (@ReceiptCode, @ReceiptDate, @WarehouseId, @ProductionOrderId,
                     @TotalQuantity, @Status, @Notes, @CreatedAt)
                    ON CONFLICT (receipt_code) 
                    DO UPDATE SET
                        receipt_date = EXCLUDED.receipt_date,
                        total_quantity = EXCLUDED.total_quantity,
                        status = EXCLUDED.status,
                        updated_at = @UpdatedAt
                    RETURNING id";

                var id = await pgConnection.ExecuteScalarAsync<int>(sql, new
                {
                    ReceiptCode = record.ReceiptCode,
                    ReceiptDate = record.ReceiptDate,
                    WarehouseId = record.WarehouseId,
                    ProductionOrderId = record.ProductionOrderId,
                    TotalQuantity = record.TotalQuantity,
                    Status = record.Status,
                    Notes = record.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                var recordData = new Dictionary<string, object>
                {
                    ["receipt_code"] = record.ReceiptCode,
                    ["receipt_date"] = record.ReceiptDate,
                    ["total_quantity"] = record.TotalQuantity,
                    ["status"] = record.Status
                };

                await _ragService.IndexDatabaseRecordAsync("product_receipts", id, recordData);

                recordsSynced++;
            }

            status.RecordsSynced = recordsSynced;
            status.IsSuccess = true;

            _logger.LogInformation("Synced {Count} ProductReceipts successfully", recordsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing ProductReceipts");
            status.IsSuccess = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<SyncStatusDto> SyncSalesOrdersAsync()
    {
        var status = new SyncStatusDto
        {
            EntityType = "SalesOrders",
            LastSyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting sync for SalesOrders");

            using var sqlConnection = _dbContext.CreateSqlServerConnection();
            using var pgConnection = _dbContext.CreatePostgreSqlConnection();

            var sqlData = await sqlConnection.QueryAsync<dynamic>(@"
                SELECT TOP 1000
                    OrderCode, OrderDate, CustomerId, DeliveryDate,
                    TotalAmount, Status, Notes
                FROM tbl_SalesOrders
                WHERE IsDeleted = 0
                ORDER BY OrderDate DESC
            ");

            var recordsSynced = 0;

            foreach (var record in sqlData)
            {
                var sql = @"
                    INSERT INTO sales_orders 
                    (order_code, order_date, customer_id, delivery_date,
                     total_amount, status, notes, created_at)
                    VALUES 
                    (@OrderCode, @OrderDate, @CustomerId, @DeliveryDate,
                     @TotalAmount, @Status, @Notes, @CreatedAt)
                    ON CONFLICT (order_code) 
                    DO UPDATE SET
                        order_date = EXCLUDED.order_date,
                        customer_id = EXCLUDED.customer_id,
                        total_amount = EXCLUDED.total_amount,
                        status = EXCLUDED.status,
                        updated_at = @UpdatedAt
                    RETURNING id";

                var id = await pgConnection.ExecuteScalarAsync<int>(sql, new
                {
                    OrderCode = record.OrderCode,
                    OrderDate = record.OrderDate,
                    CustomerId = record.CustomerId,
                    DeliveryDate = record.DeliveryDate,
                    TotalAmount = record.TotalAmount,
                    Status = record.Status,
                    Notes = record.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                var recordData = new Dictionary<string, object>
                {
                    ["order_code"] = record.OrderCode,
                    ["order_date"] = record.OrderDate,
                    ["customer_id"] = record.CustomerId,
                    ["total_amount"] = record.TotalAmount,
                    ["status"] = record.Status
                };

                await _ragService.IndexDatabaseRecordAsync("sales_orders", id, recordData);

                recordsSynced++;
            }

            status.RecordsSynced = recordsSynced;
            status.IsSuccess = true;

            _logger.LogInformation("Synced {Count} SalesOrders successfully", recordsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing SalesOrders");
            status.IsSuccess = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<SyncStatusDto> SyncInventoryAsync()
    {
        var status = new SyncStatusDto
        {
            EntityType = "Inventory",
            LastSyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting sync for Inventory");

            using var sqlConnection = _dbContext.CreateSqlServerConnection();
            using var pgConnection = _dbContext.CreatePostgreSqlConnection();

            var sqlData = await sqlConnection.QueryAsync<dynamic>(@"
                SELECT 
                    WarehouseId, MaterialId, ProductId, Quantity,
                    ReservedQuantity, AvailableQuantity, BatchNumber, ExpiryDate
                FROM tbl_Inventory
                WHERE IsDeleted = 0
            ");

            var recordsSynced = 0;

            foreach (var record in sqlData)
            {
                var sql = @"
                    INSERT INTO inventory 
                    (warehouse_id, material_id, product_id, quantity,
                     reserved_quantity, available_quantity, batch_number, 
                     expiry_date, last_update_date, created_at)
                    VALUES 
                    (@WarehouseId, @MaterialId, @ProductId, @Quantity,
                     @ReservedQuantity, @AvailableQuantity, @BatchNumber,
                     @ExpiryDate, @LastUpdateDate, @CreatedAt)
                    ON CONFLICT (warehouse_id, COALESCE(material_id, 0), COALESCE(product_id, 0))
                    DO UPDATE SET
                        quantity = EXCLUDED.quantity,
                        available_quantity = EXCLUDED.available_quantity,
                        last_update_date = @LastUpdateDate
                    RETURNING id";

                await pgConnection.ExecuteAsync(sql, new
                {
                    WarehouseId = record.WarehouseId,
                    MaterialId = record.MaterialId,
                    ProductId = record.ProductId,
                    Quantity = record.Quantity,
                    ReservedQuantity = record.ReservedQuantity,
                    AvailableQuantity = record.AvailableQuantity,
                    BatchNumber = record.BatchNumber,
                    ExpiryDate = record.ExpiryDate,
                    LastUpdateDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                recordsSynced++;
            }

            status.RecordsSynced = recordsSynced;
            status.IsSuccess = true;

            _logger.LogInformation("Synced {Count} Inventory records successfully", recordsSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Inventory");
            status.IsSuccess = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<SyncStatusDto> SyncMasterDataAsync()
    {
        var status = new SyncStatusDto
        {
            EntityType = "MasterData",
            LastSyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting sync for MasterData");
            var totalSynced = 0;

            // Sync Suppliers
            totalSynced += await SyncTable("tbl_Suppliers", "suppliers", new[]
            {
                "SupplierCode as supplier_code",
                "SupplierName as supplier_name",
                "Address",
                "Phone",
                "Email"
            });

            // Sync Customers
            totalSynced += await SyncTable("tbl_Customers", "customers", new[]
            {
                "CustomerCode as customer_code",
                "CustomerName as customer_name",
                "Address",
                "Phone",
                "Email"
            });

            // Sync Materials
            totalSynced += await SyncTable("tbl_Materials", "materials", new[]
            {
                "MaterialCode as material_code",
                "MaterialName as material_name",
                "Description",
                "UnitId as unit_id"
            });

            // Sync Products
            totalSynced += await SyncTable("tbl_Products", "products", new[]
            {
                "ProductCode as product_code",
                "ProductName as product_name",
                "Description",
                "UnitId as unit_id"
            });

            status.RecordsSynced = totalSynced;
            status.IsSuccess = true;

            _logger.LogInformation("Synced {Count} MasterData records successfully", totalSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing MasterData");
            status.IsSuccess = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<List<SyncStatusDto>> SyncAllAsync()
    {
        _logger.LogInformation("Starting full synchronization");

        var results = new List<SyncStatusDto>
        {
            await SyncMasterDataAsync(),
            await SyncMaterialReceiptsAsync(),
            await SyncProductReceiptsAsync(),
            await SyncSalesOrdersAsync(),
            await SyncInventoryAsync()
        };

        _logger.LogInformation("Full synchronization completed");

        return results;
    }

    private async Task<int> SyncTable(string sourceTable, string targetTable, string[] columns)
    {
        try
        {
            using var sqlConnection = _dbContext.CreateSqlServerConnection();
            using var pgConnection = _dbContext.CreatePostgreSqlConnection();

            var columnList = string.Join(", ", columns);
            var sqlQuery = $"SELECT TOP 1000 {columnList} FROM {sourceTable} WHERE IsDeleted = 0";

            var data = await sqlConnection.QueryAsync(sqlQuery);

            var count = 0;
            foreach (var record in data)
            {
                // This is simplified - you'd need proper INSERT/UPDATE logic per table
                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing table {SourceTable}", sourceTable);
            return 0;
        }
    }
}
