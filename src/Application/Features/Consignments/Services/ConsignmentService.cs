using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Consignments.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Consignments.Services;

public class ConsignmentService : IConsignmentService
{
    private readonly IApplicationDbContext _context;
    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public ConsignmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrokeredConsignmentDto>> GetBrokeredAsync()
    {
        return await _context.Consignments
            .Where(item => item.Type == ConsignmentType.BrokeredPurchase)
            .OrderByDescending(item => item.ConsignmentDate)
            .Select(item => MapBrokered(item))
            .ToListAsync();
    }

    public async Task<BrokeredConsignmentDto> CreateBrokeredAsync(CreateBrokeredConsignmentRequest request)
    {
        ValidateVehicleSnapshot(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.EngineNumber, request.ChassisNumber);
        if (request.PurchasePrice <= 0)
        {
            throw new BusinessRuleException("Alim tutari sifirdan buyuk olmalidir.");
        }

        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        var consignment = new Consignment
        {
            Type = ConsignmentType.BrokeredPurchase,
            Status = ConsignmentStatus.Completed,
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone?.Trim(),
            CustomerName = request.CustomerName?.Trim(),
            CustomerPhone = request.CustomerPhone?.Trim(),
            Plate = request.Plate.Trim().ToUpperInvariant(),
            SegmentId = catalog.Segment.Id,
            Segment = catalog.Segment.Name,
            BrandId = catalog.Brand.Id,
            Brand = catalog.Brand.Name,
            ModelId = catalog.Model.Id,
            Model = catalog.Model.Name,
            Year = request.Year,
            Color = request.Color.Trim(),
            EngineNumber = request.EngineNumber.Trim(),
            ChassisNumber = request.ChassisNumber.Trim(),
            ConsignmentDate = request.ConsignmentDate,
            PurchasePrice = request.PurchasePrice,
            CommissionAmount = request.CommissionAmount,
            CommissionRate = request.CommissionRate,
            Description = request.Description?.Trim()
        };

        _context.Consignments.Add(consignment);
        await _context.SaveChangesAsync();

        var brokeredCommission = ResolveBrokeredCommission(consignment.PurchasePrice ?? 0m, consignment.CommissionAmount, consignment.CommissionRate);
        if (brokeredCommission.HasValue)
        {
            _context.Transactions.Add(new Transaction
            {
                Type = TransactionType.Income,
                Amount = brokeredCommission.Value,
                TransactionDate = consignment.ConsignmentDate,
                Description = $"[Konsinye Aracilik] {consignment.Plate}",
                PaymentMethod = PaymentMethod.Cash,
                RelatedEntityId = consignment.Id,
                RelatedEntityType = "BrokeredConsignment"
            });
            await _context.SaveChangesAsync();
        }

        return MapBrokered(consignment);
    }

    public async Task<BrokeredConsignmentDto> UpdateBrokeredAsync(Guid id, UpdateBrokeredConsignmentRequest request)
    {
        ValidateVehicleSnapshot(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.EngineNumber, request.ChassisNumber);
        if (request.PurchasePrice <= 0)
        {
            throw new BusinessRuleException("Alim tutari sifirdan buyuk olmalidir.");
        }

        var consignment = await GetConsignmentAsync(id, ConsignmentType.BrokeredPurchase);
        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        consignment.OwnerName = request.OwnerName.Trim();
        consignment.OwnerPhone = request.OwnerPhone?.Trim();
        consignment.CustomerName = request.CustomerName?.Trim();
        consignment.CustomerPhone = request.CustomerPhone?.Trim();
        consignment.Plate = request.Plate.Trim().ToUpperInvariant();
        consignment.SegmentId = catalog.Segment.Id;
        consignment.Segment = catalog.Segment.Name;
        consignment.BrandId = catalog.Brand.Id;
        consignment.Brand = catalog.Brand.Name;
        consignment.ModelId = catalog.Model.Id;
        consignment.Model = catalog.Model.Name;
        consignment.Year = request.Year;
        consignment.Color = request.Color.Trim();
        consignment.EngineNumber = request.EngineNumber.Trim();
        consignment.ChassisNumber = request.ChassisNumber.Trim();
        consignment.ConsignmentDate = request.ConsignmentDate;
        consignment.PurchasePrice = request.PurchasePrice;
        consignment.CommissionAmount = request.CommissionAmount;
        consignment.CommissionRate = request.CommissionRate;
        consignment.Description = request.Description?.Trim();

        var transaction = await _context.Transactions.FirstOrDefaultAsync(item =>
            item.RelatedEntityType == "BrokeredConsignment" &&
            item.RelatedEntityId == consignment.Id);

        var brokeredCommission = ResolveBrokeredCommission(consignment.PurchasePrice ?? 0m, request.CommissionAmount, request.CommissionRate);
        if (brokeredCommission.HasValue)
        {
            if (transaction == null)
            {
                _context.Transactions.Add(new Transaction
                {
                    Type = TransactionType.Income,
                    Amount = brokeredCommission.Value,
                    TransactionDate = request.ConsignmentDate,
                    Description = $"[Konsinye Aracilik] {consignment.Plate}",
                    PaymentMethod = PaymentMethod.Cash,
                    RelatedEntityId = consignment.Id,
                    RelatedEntityType = "BrokeredConsignment"
                });
            }
            else
            {
                transaction.Amount = brokeredCommission.Value;
                transaction.TransactionDate = request.ConsignmentDate;
                transaction.Description = $"[Konsinye Aracilik] {consignment.Plate}";
                transaction.IsDeleted = false;
            }
        }
        else if (transaction != null)
        {
            transaction.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        return MapBrokered(consignment);
    }

    public async Task DeleteBrokeredAsync(Guid id)
    {
        var consignment = await GetConsignmentAsync(id, ConsignmentType.BrokeredPurchase);
        consignment.IsDeleted = true;

        var transaction = await _context.Transactions.FirstOrDefaultAsync(item =>
            item.RelatedEntityType == "BrokeredConsignment" &&
            item.RelatedEntityId == consignment.Id);

        if (transaction != null)
        {
            transaction.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<StockConsignmentDto>> GetStockAsync()
    {
        return await _context.Consignments
            .Where(item => item.Type == ConsignmentType.StockConsignment)
            .OrderByDescending(item => item.Status == ConsignmentStatus.Open)
            .ThenByDescending(item => item.SaleDate ?? item.ConsignmentDate)
            .Select(item => MapStock(item))
            .ToListAsync();
    }

    public async Task<StockConsignmentDto> CreateStockAsync(CreateStockConsignmentRequest request)
    {
        ValidateVehicleSnapshot(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.EngineNumber, request.ChassisNumber);
        if (request.BasePrice <= 0)
        {
            throw new BusinessRuleException("Konsinye baz tutar sifirdan buyuk olmalidir.");
        }

        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        var consignment = new Consignment
        {
            Type = ConsignmentType.StockConsignment,
            Status = ConsignmentStatus.Open,
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone?.Trim(),
            Plate = request.Plate.Trim().ToUpperInvariant(),
            SegmentId = catalog.Segment.Id,
            Segment = catalog.Segment.Name,
            BrandId = catalog.Brand.Id,
            Brand = catalog.Brand.Name,
            ModelId = catalog.Model.Id,
            Model = catalog.Model.Name,
            Year = request.Year,
            Color = request.Color.Trim(),
            EngineNumber = request.EngineNumber.Trim(),
            ChassisNumber = request.ChassisNumber.Trim(),
            ConsignmentDate = request.ConsignmentDate,
            PurchasePrice = request.BasePrice,
            ExpectedSalePrice = request.ExpectedSalePrice,
            CommissionAmount = request.CommissionAmount,
            CommissionRate = request.CommissionRate,
            Description = request.Description?.Trim()
        };

        _context.Consignments.Add(consignment);
        await _context.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            Plate = consignment.Plate,
            OwnershipType = VehicleOwnershipType.Consignment,
            ConsignmentId = consignment.Id,
            SegmentId = consignment.SegmentId,
            Segment = consignment.Segment,
            BrandId = consignment.BrandId,
            Brand = consignment.Brand,
            ModelId = consignment.ModelId,
            Model = consignment.Model,
            Year = consignment.Year,
            Color = consignment.Color,
            EngineNumber = consignment.EngineNumber,
            ChassisNumber = consignment.ChassisNumber,
            PurchaseDate = consignment.ConsignmentDate,
            PurchasePrice = consignment.PurchasePrice ?? 0m,
            TargetSalePrice = consignment.ExpectedSalePrice,
            Status = VehicleStatus.InStock,
            Description = $"Konsinye stok / {consignment.OwnerName}"
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        consignment.VehicleId = vehicle.Id;
        await _context.SaveChangesAsync();

        return MapStock(consignment);
    }

    public async Task<StockConsignmentDto> UpdateStockAsync(Guid id, UpdateStockConsignmentRequest request)
    {
        ValidateVehicleSnapshot(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.EngineNumber, request.ChassisNumber);
        if (request.BasePrice <= 0)
        {
            throw new BusinessRuleException("Konsinye baz tutar sifirdan buyuk olmalidir.");
        }

        var consignment = await GetConsignmentAsync(id, ConsignmentType.StockConsignment);
        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        consignment.OwnerName = request.OwnerName.Trim();
        consignment.OwnerPhone = request.OwnerPhone?.Trim();
        consignment.Plate = request.Plate.Trim().ToUpperInvariant();
        consignment.SegmentId = catalog.Segment.Id;
        consignment.Segment = catalog.Segment.Name;
        consignment.BrandId = catalog.Brand.Id;
        consignment.Brand = catalog.Brand.Name;
        consignment.ModelId = catalog.Model.Id;
        consignment.Model = catalog.Model.Name;
        consignment.Year = request.Year;
        consignment.Color = request.Color.Trim();
        consignment.EngineNumber = request.EngineNumber.Trim();
        consignment.ChassisNumber = request.ChassisNumber.Trim();
        consignment.ConsignmentDate = request.ConsignmentDate;
        consignment.PurchasePrice = request.BasePrice;
        consignment.ExpectedSalePrice = request.ExpectedSalePrice;
        consignment.CommissionAmount = request.CommissionAmount;
        consignment.CommissionRate = request.CommissionRate;
        consignment.Description = request.Description?.Trim();

        var vehicle = consignment.VehicleId.HasValue
            ? await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == consignment.VehicleId.Value)
            : null;

        if (vehicle != null)
        {
            vehicle.Plate = consignment.Plate;
            vehicle.SegmentId = consignment.SegmentId;
            vehicle.Segment = consignment.Segment;
            vehicle.BrandId = consignment.BrandId;
            vehicle.Brand = consignment.Brand;
            vehicle.ModelId = consignment.ModelId;
            vehicle.Model = consignment.Model;
            vehicle.Year = consignment.Year;
            vehicle.Color = consignment.Color;
            vehicle.EngineNumber = consignment.EngineNumber;
            vehicle.ChassisNumber = consignment.ChassisNumber;
            vehicle.PurchaseDate = consignment.ConsignmentDate;
            vehicle.PurchasePrice = consignment.PurchasePrice ?? 0m;
            vehicle.TargetSalePrice = consignment.ExpectedSalePrice;
            vehicle.Description = $"Konsinye stok / {consignment.OwnerName}";
        }

        await _context.SaveChangesAsync();
        return MapStock(consignment);
    }

    public async Task<StockConsignmentDto> CompleteStockSaleAsync(Guid id, CompleteStockConsignmentSaleRequest request)
    {
        return await UpsertStockSaleAsync(id, request, false);
    }

    public async Task<StockConsignmentDto> UpdateStockSaleAsync(Guid id, CompleteStockConsignmentSaleRequest request)
    {
        return await UpsertStockSaleAsync(id, request, true);
    }

    public async Task DeleteStockAsync(Guid id)
    {
        var consignment = await GetConsignmentAsync(id, ConsignmentType.StockConsignment);
        if (consignment.Status == ConsignmentStatus.Sold)
        {
            throw new BusinessRuleException("Satilmis konsinye stok kaydi silinemez.");
        }

        if (consignment.VehicleId.HasValue)
        {
            var hasExpenses = await _context.VehicleExpenses.AnyAsync(item => item.VehicleId == consignment.VehicleId.Value);
            if (hasExpenses)
            {
                throw new BusinessRuleException("Masraf girilmis konsinye stok kaydi silinemez.");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == consignment.VehicleId.Value);
            if (vehicle != null)
            {
                vehicle.IsDeleted = true;
            }
        }

        consignment.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteStockSaleAsync(Guid id)
    {
        var consignment = await GetConsignmentAsync(id, ConsignmentType.StockConsignment);
        if (consignment.Status != ConsignmentStatus.Sold)
        {
            throw new BusinessRuleException("Silinecek aktif bir konsinye satisi bulunamadi.");
        }

        consignment.Status = ConsignmentStatus.Open;
        consignment.SalePrice = null;
        consignment.NetAmountToOwner = null;
        consignment.SaleDate = null;

        if (consignment.VehicleId.HasValue)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == consignment.VehicleId.Value);
            if (vehicle != null)
            {
                vehicle.Status = VehicleStatus.InStock;
                vehicle.ActualSalePrice = null;
                vehicle.SaleDate = null;
            }
        }

        var transaction = await _context.Transactions.FirstOrDefaultAsync(item =>
            item.RelatedEntityType == "StockConsignmentSale" &&
            item.RelatedEntityId == consignment.Id);

        if (transaction != null)
        {
            transaction.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<StockConsignmentDto> UpsertStockSaleAsync(Guid id, CompleteStockConsignmentSaleRequest request, bool updateExisting)
    {
        if (request.SalePrice <= 0)
        {
            throw new BusinessRuleException("Satis tutari sifirdan buyuk olmalidir.");
        }

        var consignment = await GetConsignmentAsync(id, ConsignmentType.StockConsignment);
        if (!updateExisting && consignment.Status == ConsignmentStatus.Sold)
        {
            throw new BusinessRuleException("Bu konsinye stok zaten satilmis.");
        }

        if (updateExisting && consignment.Status != ConsignmentStatus.Sold)
        {
            throw new BusinessRuleException("Duzenlenecek aktif bir konsinye satisi bulunamadi.");
        }

        var commissionAmount = ResolveCommission(consignment, request.SalePrice, request.CommissionAmount, request.CommissionRate);
        var netAmountToOwner = request.SalePrice - commissionAmount;

        consignment.Status = ConsignmentStatus.Sold;
        consignment.SalePrice = request.SalePrice;
        consignment.SaleDate = request.SaleDate;
        consignment.NetAmountToOwner = netAmountToOwner;
        consignment.CommissionAmount = request.CommissionAmount ?? commissionAmount;
        consignment.CommissionRate = request.CommissionRate ?? consignment.CommissionRate;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            consignment.Description = request.Description.Trim();
        }

        if (consignment.VehicleId.HasValue)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == consignment.VehicleId.Value);
            if (vehicle != null)
            {
                vehicle.Status = VehicleStatus.Sold;
                vehicle.ActualSalePrice = request.SalePrice;
                vehicle.SaleDate = request.SaleDate;
                vehicle.Description = $"Konsinye stok / {consignment.OwnerName}";
            }
        }

        var transaction = await _context.Transactions.FirstOrDefaultAsync(item =>
            item.RelatedEntityType == "StockConsignmentSale" &&
            item.RelatedEntityId == consignment.Id);

        if (transaction == null)
        {
            _context.Transactions.Add(new Transaction
            {
                Type = TransactionType.Income,
                Amount = commissionAmount,
                TransactionDate = request.SaleDate,
                Description = $"[Konsinye Satis] {consignment.Plate}",
                PaymentMethod = PaymentMethod.Cash,
                RelatedEntityId = consignment.Id,
                RelatedEntityType = "StockConsignmentSale"
            });
        }
        else
        {
            transaction.Amount = commissionAmount;
            transaction.TransactionDate = request.SaleDate;
            transaction.Description = $"[Konsinye Satis] {consignment.Plate}";
            transaction.IsDeleted = false;
        }

        await _context.SaveChangesAsync();
        return MapStock(consignment);
    }

    private static decimal ResolveCommission(
        Consignment consignment,
        decimal salePrice,
        decimal? requestCommissionAmount,
        decimal? requestCommissionRate)
    {
        if (requestCommissionAmount.HasValue && requestCommissionAmount.Value > 0)
        {
            return requestCommissionAmount.Value;
        }

        if (requestCommissionRate.HasValue && requestCommissionRate.Value > 0)
        {
            return Math.Round((salePrice * requestCommissionRate.Value) / 100m, 2);
        }

        if (consignment.CommissionAmount.HasValue && consignment.CommissionAmount.Value > 0)
        {
            return consignment.CommissionAmount.Value;
        }

        if (consignment.CommissionRate.HasValue && consignment.CommissionRate.Value > 0)
        {
            return Math.Round((salePrice * consignment.CommissionRate.Value) / 100m, 2);
        }

        throw new BusinessRuleException("Komisyon tutari veya komisyon orani girmeniz gerekiyor.");
    }

    private static decimal? ResolveBrokeredCommission(decimal purchasePrice, decimal? commissionAmount, decimal? commissionRate)
    {
        if (commissionAmount.HasValue && commissionAmount.Value > 0)
        {
            return commissionAmount.Value;
        }

        if (commissionRate.HasValue && commissionRate.Value > 0)
        {
            return Math.Round((purchasePrice * commissionRate.Value) / 100m, 2);
        }

        return null;
    }

    private async Task<Consignment> GetConsignmentAsync(Guid id, ConsignmentType type)
    {
        var consignment = await _context.Consignments.FirstOrDefaultAsync(item => item.Id == id && item.Type == type);
        if (consignment == null)
        {
            throw new BusinessRuleException("Konsinye kaydi bulunamadi.");
        }

        return consignment;
    }

    private async Task<(VehicleSegment Segment, VehicleBrand Brand, VehicleCatalogModel Model)> ResolveCatalogAsync(
        Guid segmentId,
        Guid brandId,
        Guid modelId)
    {
        var segment = await _context.VehicleSegments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == segmentId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == brandId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        var model = await _context.VehicleCatalogModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == modelId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);

        if (segment == null || brand == null || model == null)
        {
            throw new BusinessRuleException("Secilen segment, marka veya model bulunamadi.");
        }

        if (model.VehicleBrandId != brand.Id)
        {
            throw new BusinessRuleException("Secilen model secilen markaya ait degil.");
        }

        if (model.VehicleSegmentId.HasValue && model.VehicleSegmentId != segment.Id)
        {
            throw new BusinessRuleException("Secilen model secilen segment ile uyumlu degil.");
        }

        return (segment, brand, model);
    }

    private static void ValidateVehicleSnapshot(string plate, Guid segmentId, Guid brandId, Guid modelId, string engineNumber, string chassisNumber)
    {
        if (string.IsNullOrWhiteSpace(plate) || segmentId == Guid.Empty || brandId == Guid.Empty || modelId == Guid.Empty)
        {
            throw new BusinessRuleException("Plaka, segment, marka ve model zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(engineNumber) || string.IsNullOrWhiteSpace(chassisNumber))
        {
            throw new BusinessRuleException("Motor ve sasi numarasi zorunludur.");
        }
    }

    private static BrokeredConsignmentDto MapBrokered(Consignment item) =>
        new(
            item.Id,
            item.OwnerName,
            item.OwnerPhone,
            item.CustomerName,
            item.CustomerPhone,
            item.Plate,
            item.SegmentId,
            item.Segment,
            item.BrandId,
            item.Brand,
            item.ModelId,
            item.Model,
            item.Year,
            item.Color,
            item.EngineNumber,
            item.ChassisNumber,
            item.ConsignmentDate,
            item.PurchasePrice ?? 0m,
            item.CommissionAmount,
            item.CommissionRate,
            item.Status,
            item.Description
        );

    private static StockConsignmentDto MapStock(Consignment item) =>
        new(
            item.Id,
            item.VehicleId,
            item.OwnerName,
            item.OwnerPhone,
            item.Plate,
            item.SegmentId,
            item.Segment,
            item.BrandId,
            item.Brand,
            item.ModelId,
            item.Model,
            item.Year,
            item.Color,
            item.EngineNumber,
            item.ChassisNumber,
            item.ConsignmentDate,
            item.PurchasePrice ?? 0m,
            item.ExpectedSalePrice,
            item.CommissionAmount,
            item.CommissionRate,
            item.SalePrice,
            item.NetAmountToOwner,
            item.SaleDate,
            item.Status,
            item.Description
        );
}
