using AutoGallerySaaS.Domain.Common;

namespace AutoGallerySaaS.Domain.Entities.Vehicles;

public class Vehicle : BaseTenantEntity
{
    public string Plate { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string Color { get; set; } = null!;
    public int Kilometer { get; set; }
    public string EngineNumber { get; set; } = null!;
    public string ChassisNumber { get; set; } = null!;
    public decimal PurchasePrice { get; set; }
    public decimal? TargetSalePrice { get; set; }
    public decimal? ActualSalePrice { get; set; }
    public VehicleStatus Status { get; set; }
    public string? Description { get; set; }

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();
    public ICollection<VehicleAttachment> Attachments { get; set; } = new List<VehicleAttachment>();
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
