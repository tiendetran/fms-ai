namespace FAS.Core.Entities;


/// <summary>
/// Nguyên liệu
/// </summary>
public class Material : BaseEntity
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UnitId { get; set; }
    public decimal StandardQuantity { get; set; }
    public string? Specifications { get; set; }
}

/// <summary>
/// Thành phẩm
/// </summary>
public class Product : BaseEntity
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UnitId { get; set; }
    public decimal StandardPrice { get; set; }
    public string? Specifications { get; set; }
}

/// <summary>
/// Đơn vị tính
/// </summary>
public class Unit : BaseEntity
{
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Nhà cung cấp
/// </summary>
public class Supplier : BaseEntity
{
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? TaxCode { get; set; }
}

/// <summary>
/// Nhà sản xuất
/// </summary>
public class Manufacturer : BaseEntity
{
    public string ManufacturerCode { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
}

/// <summary>
/// Khách hàng
/// </summary>
public class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? TaxCode { get; set; }
}

/// <summary>
/// Nhà kho
/// </summary>
public class Warehouse : BaseEntity
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? WarehouseType { get; set; } // NguyenLieu, SanXuat, ThanhPham
    public decimal Capacity { get; set; }
}

/// <summary>
/// Phiếu nhập nguyên liệu
/// </summary>
public class MaterialReceipt : BaseEntity
{
    public string ReceiptCode { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Completed
    public string? Notes { get; set; }
}

/// <summary>
/// Chi tiết phiếu nhập nguyên liệu
/// </summary>
public class MaterialReceiptDetail : BaseEntity
{
    public int ReceiptId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? QualityStatus { get; set; } // Pending, Passed, Failed
}

/// <summary>
/// Phiếu nhập thành phẩm
/// </summary>
public class ProductReceipt : BaseEntity
{
    public string ReceiptCode { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int WarehouseId { get; set; }
    public int ProductionOrderId { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Chi tiết phiếu nhập thành phẩm
/// </summary>
public class ProductReceiptDetail : BaseEntity
{
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? QualityStatus { get; set; }
}

/// <summary>
/// Đơn hàng
/// </summary>
public class SalesOrder : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Draft, Confirmed, InProduction, Completed
    public string? Notes { get; set; }
}

/// <summary>
/// Chi tiết đơn hàng
/// </summary>
public class SalesOrderDetail : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Phiếu xuất bán hàng
/// </summary>
public class SalesDelivery : BaseEntity
{
    public string DeliveryCode { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Chi tiết phiếu xuất bán hàng
/// </summary>
public class SalesDeliveryDetail : BaseEntity
{
    public int DeliveryId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNumber { get; set; }
}

/// <summary>
/// Tồn kho
/// </summary>
public class Inventory : BaseEntity
{
    public int WarehouseId { get; set; }
    public int? MaterialId { get; set; }
    public int? ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
}

/// <summary>
/// Kế hoạch sản xuất
/// </summary>
public class ProductionPlan : BaseEntity
{
    public string PlanCode { get; set; } = string.Empty;
    public DateTime PlanDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Lệnh sản xuất
/// </summary>
public class ProductionOrder : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public int PlanId { get; set; }
    public int ProductId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
