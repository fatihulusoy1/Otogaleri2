using AutoGallerySaaS.Domain.Common;
using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Domain.Entities.Vehicles;

public class Vehicle : BaseTenantEntity
{
    public string Plate { get; set; } = null!;
    public VehicleOwnershipType OwnershipType { get; set; } = VehicleOwnershipType.Owned;
    public Guid? ConsignmentId { get; set; }
    public Guid? SegmentId { get; set; }
    public VehicleSegment? SegmentLookup { get; set; }
    public string? Segment { get; set; }
    public Guid? BrandId { get; set; }
    public VehicleBrand? BrandLookup { get; set; }
    public string Brand { get; set; } = null!;
    public Guid? ModelId { get; set; }
    public VehicleCatalogModel? ModelLookup { get; set; }
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string Color { get; set; } = null!;
    public int Kilometer { get; set; }
    public string EngineNumber { get; set; } = null!;
    public string ChassisNumber { get; set; } = null!;
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public PaymentMethod PurchasePaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? PurchaseNotaryRegistryNumber { get; set; }
    public string? PurchaseTradePlate { get; set; }
    public decimal? PurchaseTradeAmount { get; set; }
    public decimal? TargetSalePrice { get; set; }
    public decimal? ActualSalePrice { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? SaleTradePlate { get; set; }
    public decimal? SaleTradeAmount { get; set; }
    public VehicleStatus Status { get; set; }
    public string? Description { get; set; }

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();
    public ICollection<VehicleAttachment> Attachments { get; set; } = new List<VehicleAttachment>();
}

public class VehicleSegment : BaseTenantEntity
{
    public string Name { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<VehicleCatalogModel> Models { get; set; } = new List<VehicleCatalogModel>();
}

public class VehicleBrand : BaseTenantEntity
{
    public string Name { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<VehicleCatalogModel> Models { get; set; } = new List<VehicleCatalogModel>();
}

public class VehicleCatalogModel : BaseTenantEntity
{
    public string Name { get; set; } = null!;
    public Guid VehicleBrandId { get; set; }
    public VehicleBrand VehicleBrand { get; set; } = null!;
    public Guid? VehicleSegmentId { get; set; }
    public VehicleSegment? VehicleSegment { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

public enum VehicleStatus
{
    InStock = 1,
    Sold = 2,
    Reserved = 3,
    InMaintenance = 4
}

public class VehicleExpense : BaseTenantEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
}

public class VehicleAttachment : BaseTenantEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string FileType { get; set; } = null!; // Image, Document
    public long FileSize { get; set; }
}

public class VehicleTrade : BaseTenantEntity
{
    public VehicleTradeOperationType OperationType { get; set; }
    public Guid SourceVehicleId { get; set; }
    public Guid TradeVehicleId { get; set; }
    public decimal AgreedValue { get; set; }
    public decimal CashAmount { get; set; }
    public string? Description { get; set; }
}

public enum VehicleTradeOperationType
{
    Purchase = 1,
    Sale = 2
}
