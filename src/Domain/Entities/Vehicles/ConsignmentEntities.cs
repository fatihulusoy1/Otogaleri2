using AutoGallerySaaS.Domain.Common;

namespace AutoGallerySaaS.Domain.Entities.Vehicles;

public class Consignment : BaseTenantEntity
{
    public ConsignmentType Type { get; set; }
    public ConsignmentStatus Status { get; set; }

    public Guid? VehicleId { get; set; }
    public Guid? SegmentId { get; set; }
    public string? Segment { get; set; }
    public Guid? BrandId { get; set; }
    public string Brand { get; set; } = null!;
    public Guid? ModelId { get; set; }
    public string Model { get; set; } = null!;
    public string Plate { get; set; } = null!;
    public int Year { get; set; }
    public string Color { get; set; } = null!;
    public string EngineNumber { get; set; } = null!;
    public string ChassisNumber { get; set; } = null!;

    public string OwnerName { get; set; } = null!;
    public string? OwnerPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public DateTime ConsignmentDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? ExpectedSalePrice { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? CommissionRate { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? NetAmountToOwner { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? Description { get; set; }
}

public enum ConsignmentType
{
    BrokeredPurchase = 1,
    StockConsignment = 2
}

public enum ConsignmentStatus
{
    Open = 1,
    Completed = 2,
    Sold = 3,
    Cancelled = 4
}

public enum VehicleOwnershipType
{
    Owned = 1,
    Consignment = 2
}
