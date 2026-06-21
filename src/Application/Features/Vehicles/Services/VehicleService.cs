using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Subscriptions.Services;
using AutoGallerySaaS.Application.Features.Vehicles.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public class VehicleService : IVehicleService
{
    private readonly IApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public VehicleService(IApplicationDbContext context, ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    public async Task<VehicleLookupsDto> GetLookupsAsync()
    {
        await EnsureVehicleLookupsAsync();

        var segments = await _context.VehicleSegments
            .IgnoreQueryFilters()
            .Where(segment => segment.TenantId == SharedLookupTenantId && !segment.IsDeleted)
            .OrderBy(segment => segment.Name)
            .Select(segment => new LookupOptionDto(segment.Id, segment.Name))
            .ToListAsync();

        var brands = await _context.VehicleBrands
            .IgnoreQueryFilters()
            .Where(brand => brand.TenantId == SharedLookupTenantId && !brand.IsDeleted)
            .OrderBy(brand => brand.Name)
            .Select(brand => new LookupOptionDto(brand.Id, brand.Name))
            .ToListAsync();

        var models = await _context.VehicleCatalogModels
            .IgnoreQueryFilters()
            .Where(model => model.TenantId == SharedLookupTenantId && !model.IsDeleted)
            .OrderBy(model => model.Name)
            .Select(model => new VehicleModelLookupDto(
                model.Id,
                model.Name,
                model.VehicleBrandId,
                model.VehicleSegmentId))
            .ToListAsync();

        return new VehicleLookupsDto(segments, brands, models);
    }

    public async Task<List<VehicleDto>> GetAllAsync()
    {
        var vehicles = await _context.Vehicles
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .ToListAsync();

        var expenseLookup = await GetVehicleExpenseLookupAsync(vehicles.Select(vehicle => vehicle.Id).ToList());
        var consignmentCommissionLookup = await GetConsignmentCommissionLookupAsync(vehicles);
        return vehicles.Select(vehicle => MapVehicle(vehicle, expenseLookup, consignmentCommissionLookup)).ToList();
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return null;
        }

        var expenseLookup = await GetVehicleExpenseLookupAsync(new List<Guid> { vehicle.Id });
        var consignmentCommissionLookup = await GetConsignmentCommissionLookupAsync(new List<Vehicle> { vehicle });
        return MapVehicle(vehicle, expenseLookup, consignmentCommissionLookup);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        var vehicle = await CreateOwnedVehicleAsync(
            request.Plate,
            request.SegmentId,
            request.BrandId,
            request.ModelId,
            request.Year,
            request.Color,
            request.EngineNumber,
            request.ChassisNumber,
            request.PurchaseDate,
            request.PurchasePrice,
            request.TargetSalePrice,
            request.Description);

        return MapVehicle(
            vehicle,
            new Dictionary<Guid, decimal> { [vehicle.Id] = 0m },
            new Dictionary<Guid, decimal?>());
    }

    public async Task<PurchaseRecordDto> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        ValidatePurchaseTradeRequest(request.PurchasePrice, request.TradeAmount);
        var vehicle = await CreateOwnedVehicleAsync(
            request.Plate,
            request.SegmentId,
            request.BrandId,
            request.ModelId,
            request.Year,
            request.Color,
            request.EngineNumber,
            request.ChassisNumber,
            request.PurchaseDate,
            request.PurchasePrice,
            request.TargetSalePrice,
            request.Description);

        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);
        vehicle.PurchaseTradePlate = request.PaymentMethod == PaymentMethod.Trade
            ? NormalizeOptionalText(request.TradePlate)
            : null;
        vehicle.PurchaseTradeAmount = request.PaymentMethod == PaymentMethod.Trade
            ? request.TradeAmount
            : null;
        vehicle.PurchasePaymentMethod = request.PaymentMethod;
        vehicle.PurchaseNotaryRegistryNumber = NormalizeOptionalText(request.NotaryRegistryNumber);
        await _context.SaveChangesAsync();
        await SyncPurchaseReceivablePayableAsync(vehicle, request.PaymentMethod, request.CounterpartyName, request.DueDate, request.DocumentNumber, request.InstallmentCount, request.InstallmentIntervalMonths);

        return MapPurchaseRecord(vehicle, await GetReceivablePayableAsync(ReceivablePayableSourceType.VehiclePurchase, vehicle.Id));
    }

    private async Task<Vehicle> CreateOwnedVehicleAsync(
        string plate,
        Guid segmentId,
        Guid brandId,
        Guid modelId,
        int year,
        string color,
        string engineNumber,
        string chassisNumber,
        DateTime purchaseDate,
        decimal purchasePrice,
        decimal? targetSalePrice,
        string? description)
    {
        ValidateVehicleRequest(plate, segmentId, brandId, modelId, purchasePrice);
        await _subscriptionService.EnsureCanAddVehicleAsync();
        var catalog = await ResolveCatalogAsync(segmentId, brandId, modelId);

        var vehicle = new Vehicle
        {
            Plate = plate.Trim().ToUpperInvariant(),
            OwnershipType = VehicleOwnershipType.Owned,
            SegmentId = catalog.Segment.Id,
            Segment = catalog.Segment.Name,
            BrandId = catalog.Brand.Id,
            Brand = catalog.Brand.Name,
            ModelId = catalog.Model.Id,
            Model = catalog.Model.Name,
            Year = year,
            Color = color.Trim(),
            EngineNumber = engineNumber.Trim(),
            ChassisNumber = chassisNumber.Trim(),
            PurchaseDate = purchaseDate,
            PurchasePrice = purchasePrice,
            TargetSalePrice = targetSalePrice,
            Status = VehicleStatus.InStock,
            Description = description
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return vehicle;
    }

    public async Task<List<PurchaseRecordDto>> GetPurchasesAsync()
    {
        var vehicles = await _context.Vehicles
            .Where(vehicle => vehicle.OwnershipType == VehicleOwnershipType.Owned)
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .ToListAsync();
        var financeLookup = await GetReceivablePayableLookupAsync(ReceivablePayableSourceType.VehiclePurchase, vehicles.Select(vehicle => vehicle.Id).ToList());
        return vehicles.Select(vehicle => MapPurchaseRecord(vehicle, financeLookup.GetValueOrDefault(vehicle.Id))).ToList();
    }

    public async Task<PurchaseRecordDto> UpdatePurchaseAsync(Guid vehicleId, UpdatePurchaseRequest request)
    {
        ValidateVehicleRequest(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.PurchasePrice);
        ValidatePurchaseTradeRequest(request.PurchasePrice, request.TradeAmount);

        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new NotFoundException("Vehicle not found");
        }

        vehicle.Plate = request.Plate.Trim().ToUpperInvariant();
        vehicle.SegmentId = catalog.Segment.Id;
        vehicle.Segment = catalog.Segment.Name;
        vehicle.BrandId = catalog.Brand.Id;
        vehicle.Brand = catalog.Brand.Name;
        vehicle.ModelId = catalog.Model.Id;
        vehicle.Model = catalog.Model.Name;
        vehicle.Year = request.Year;
        vehicle.Color = request.Color.Trim();
        vehicle.EngineNumber = request.EngineNumber.Trim();
        vehicle.ChassisNumber = request.ChassisNumber.Trim();
        vehicle.PurchaseDate = request.PurchaseDate;
        vehicle.PurchasePrice = request.PurchasePrice;
        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);
        vehicle.PurchasePaymentMethod = request.PaymentMethod;
        vehicle.PurchaseNotaryRegistryNumber = NormalizeOptionalText(request.NotaryRegistryNumber);
        vehicle.PurchaseTradePlate = request.PaymentMethod == PaymentMethod.Trade
            ? NormalizeOptionalText(request.TradePlate)
            : null;
        vehicle.PurchaseTradeAmount = request.PaymentMethod == PaymentMethod.Trade
            ? request.TradeAmount
            : null;
        vehicle.TargetSalePrice = request.TargetSalePrice;
        vehicle.Description = request.Description;

        await _context.SaveChangesAsync();
        await SyncPurchaseReceivablePayableAsync(vehicle, request.PaymentMethod, request.CounterpartyName, request.DueDate, request.DocumentNumber, request.InstallmentCount, request.InstallmentIntervalMonths);

        return MapPurchaseRecord(vehicle, await GetReceivablePayableAsync(ReceivablePayableSourceType.VehiclePurchase, vehicle.Id));
    }

    public async Task DeletePurchaseAsync(Guid vehicleId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId);
        if (vehicle == null)
        {
            return;
        }

        var hasExpenses = await _context.VehicleExpenses.AnyAsync(item => item.VehicleId == vehicleId);
        if (hasExpenses)
        {
            throw new BusinessRuleException("Bu satinalma silinemez. Araca bagli masraf kaydi bulunuyor.");
        }

        var hasSale = vehicle.Status == VehicleStatus.Sold ||
                      vehicle.ActualSalePrice.HasValue ||
                      await _context.Transactions.AnyAsync(item =>
                          item.RelatedEntityType == "VehicleSale" &&
                          item.RelatedEntityId == vehicleId);

        if (hasSale)
        {
            throw new BusinessRuleException("Bu satinalma silinemez. Araca ait satis kaydi bulunuyor.");
        }

        vehicle.IsDeleted = true;
        await SoftDeleteReceivablePayableAsync(ReceivablePayableSourceType.VehiclePurchase, vehicleId);
        await _context.SaveChangesAsync();
    }

    public async Task<VehicleExpenseDto> AddExpenseAsync(Guid vehicleId, CreateVehicleExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Expense amount must be greater than zero");
        }

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new NotFoundException("Vehicle not found");
        }

        if (request.CategoryId.HasValue)
        {
            var isValidCategory = await _context.VehicleExpenseCategories
                .IgnoreQueryFilters()
                .AnyAsync(category =>
                    category.Id == request.CategoryId.Value &&
                    category.TenantId == SharedLookupTenantId &&
                    !category.IsDeleted);

            if (!isValidCategory)
            {
                throw new ValidationException("Selected category is not valid for vehicle expenses");
            }
        }

        var expense = new VehicleExpense
        {
            VehicleId = vehicle.Id,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate
        };
        _context.VehicleExpenses.Add(expense);
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = request.Amount,
            TransactionDate = request.ExpenseDate,
            Description = $"[{vehicle.Plate}] {request.Description.Trim()}",
            PaymentMethod = request.PaymentMethod,
            CategoryId = request.CategoryId,
            RelatedEntityId = expense.Id,
            RelatedEntityType = "VehicleExpense"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new VehicleExpenseDto(
            expense.Id,
            expense.VehicleId,
            vehicle.Plate,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            request.CategoryId,
            null,
            request.PaymentMethod);
    }

    public async Task<VehicleExpenseDto> UpdateExpenseAsync(Guid expenseId, UpdateVehicleExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Expense amount must be greater than zero");
        }

        if (request.CategoryId.HasValue)
        {
            var isValidCategory = await _context.VehicleExpenseCategories
                .IgnoreQueryFilters()
                .AnyAsync(category =>
                    category.Id == request.CategoryId.Value &&
                    category.TenantId == SharedLookupTenantId &&
                    !category.IsDeleted);

            if (!isValidCategory)
            {
                throw new ValidationException("Selected category is not valid for vehicle expenses");
            }
        }

        var expense = await _context.VehicleExpenses
            .Join(
                _context.Vehicles,
                currentExpense => currentExpense.VehicleId,
                vehicle => vehicle.Id,
                (currentExpense, vehicle) => new { Expense = currentExpense, Vehicle = vehicle })
            .FirstOrDefaultAsync(item => item.Expense.Id == expenseId);

        if (expense == null)
        {
            throw new NotFoundException("Expense not found");
        }

        expense.Expense.Description = request.Description.Trim();
        expense.Expense.Amount = request.Amount;
        expense.Expense.ExpenseDate = request.ExpenseDate;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(item =>
                item.RelatedEntityType == "VehicleExpense" &&
                item.RelatedEntityId == expense.Expense.Id);

        if (transaction != null)
        {
            transaction.Amount = request.Amount;
            transaction.TransactionDate = request.ExpenseDate;
            transaction.Description = $"[{expense.Vehicle.Plate}] {request.Description.Trim()}";
            transaction.PaymentMethod = request.PaymentMethod;
            transaction.CategoryId = request.CategoryId;
        }

        await _context.SaveChangesAsync();

        string? categoryName = null;
        if (request.CategoryId.HasValue)
        {
            categoryName = await _context.VehicleExpenseCategories
                .IgnoreQueryFilters()
                .Where(category => category.TenantId == SharedLookupTenantId && !category.IsDeleted)
                .Where(category => category.Id == request.CategoryId.Value)
                .Select(category => category.Name)
                .FirstOrDefaultAsync();
        }

        return new VehicleExpenseDto(
            expense.Expense.Id,
            expense.Expense.VehicleId,
            expense.Vehicle.Plate,
            expense.Expense.Description,
            expense.Expense.Amount,
            expense.Expense.ExpenseDate,
            request.CategoryId,
            categoryName,
            request.PaymentMethod);
    }

    public async Task<List<VehicleExpenseDto>> GetExpensesAsync(Guid? vehicleId = null)
    {
        var expensesQuery = _context.VehicleExpenses
            .Join(
                _context.Vehicles,
                expense => expense.VehicleId,
                vehicle => vehicle.Id,
                (expense, vehicle) => new { expense, vehicle })
            .AsQueryable();

        if (vehicleId.HasValue)
        {
            expensesQuery = expensesQuery.Where(item => item.expense.VehicleId == vehicleId.Value);
        }

        var paymentLookup = await _context.Transactions
            .Where(transaction => transaction.RelatedEntityType == "VehicleExpense")
            .ToDictionaryAsync(
                transaction => transaction.RelatedEntityId ?? Guid.Empty,
                transaction => new { transaction.PaymentMethod, transaction.CategoryId });

        var categoryLookup = await _context.VehicleExpenseCategories
            .IgnoreQueryFilters()
            .Where(category => category.TenantId == SharedLookupTenantId && !category.IsDeleted)
            .ToDictionaryAsync(category => category.Id, category => category.Name);

        var expenses = await expensesQuery
            .OrderByDescending(item => item.expense.ExpenseDate)
            .Select(item => new VehicleExpenseDto(
                item.expense.Id,
                item.expense.VehicleId,
                item.vehicle.Plate,
                item.expense.Description,
                item.expense.Amount,
                item.expense.ExpenseDate,
                null,
                null,
                PaymentMethod.Cash))
            .ToListAsync();

        return expenses
            .Select(expense => expense with
            {
                PaymentMethod = paymentLookup.GetValueOrDefault(expense.Id)?.PaymentMethod ?? PaymentMethod.Cash,
                CategoryId = paymentLookup.GetValueOrDefault(expense.Id)?.CategoryId,
                CategoryName = paymentLookup.TryGetValue(expense.Id, out var item) && item.CategoryId.HasValue
                    ? categoryLookup.GetValueOrDefault(item.CategoryId.Value)
                    : null
            })
            .ToList();
    }

    public async Task DeleteExpenseAsync(Guid expenseId)
    {
        var expense = await _context.VehicleExpenses.FirstOrDefaultAsync(item => item.Id == expenseId);
        if (expense == null)
        {
            return;
        }

        expense.IsDeleted = true;

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(item =>
                item.RelatedEntityType == "VehicleExpense" &&
                item.RelatedEntityId == expenseId);

        if (transaction != null)
        {
            transaction.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<VehicleSaleDto> CompleteSaleAsync(Guid vehicleId, CompleteVehicleSaleRequest request)
    {
        if (request.SalePrice <= 0)
        {
            throw new ValidationException("Sale price must be greater than zero");
        }

        ValidateSaleTradeRequest(request.SalePrice, request.TradeAmount);
        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new NotFoundException("Vehicle not found");
        }

        if (vehicle.Status == VehicleStatus.Sold)
        {
            throw new BusinessRuleException("Vehicle is already sold");
        }

        var totalExpenseCost = await _context.VehicleExpenses
            .Where(expense => expense.VehicleId == vehicle.Id)
            .SumAsync(expense => expense.Amount);

        vehicle.Status = VehicleStatus.Sold;
        vehicle.ActualSalePrice = request.SalePrice;
        vehicle.SaleDate = request.SaleDate;
        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);
        vehicle.SaleTradePlate = request.PaymentMethod == PaymentMethod.Trade
            ? NormalizeOptionalText(request.TradePlate)
            : null;
        vehicle.SaleTradeAmount = request.PaymentMethod == PaymentMethod.Trade
            ? request.TradeAmount
            : null;
        vehicle.Description = string.IsNullOrWhiteSpace(request.Description)
            ? vehicle.Description
            : request.Description.Trim();

        _context.Transactions.Add(new Transaction
        {
            Type = TransactionType.Income,
            Amount = request.SalePrice,
            TransactionDate = request.SaleDate,
            Description = $"[{vehicle.Plate}] vehicle sale",
            PaymentMethod = request.PaymentMethod,
            RelatedEntityId = vehicle.Id,
            RelatedEntityType = "VehicleSale"
        });

        await _context.SaveChangesAsync();
        await SyncSaleReceivablePayableAsync(vehicle, request.PaymentMethod, request.CounterpartyName, request.DueDate, request.DocumentNumber, request.InstallmentCount, request.InstallmentIntervalMonths);

        var totalCost = vehicle.PurchasePrice + totalExpenseCost;
        var profit = request.SalePrice - totalCost;
        var profitMargin = totalCost == 0 ? 0 : Math.Round((profit / totalCost) * 100m, 2);

        var financeItem = await GetReceivablePayableAsync(ReceivablePayableSourceType.VehicleSale, vehicle.Id);

        return new VehicleSaleDto(
            vehicle.Id,
            vehicle.Plate,
            $"{vehicle.Brand} {vehicle.Model}",
            vehicle.PurchasePrice,
            totalExpenseCost,
            totalCost,
            request.SalePrice,
            profit,
            profitMargin,
            request.SaleDate,
            financeItem?.PaymentMethod ?? request.PaymentMethod,
            financeItem?.CounterpartyName,
            financeItem?.DueDate,
            financeItem?.DocumentNumber,
            financeItem?.PaymentMethod == PaymentMethod.PromissoryNote ? financeItem?.OriginalAmount : null,
            null,
            null,
            vehicle.SaleTradePlate,
            vehicle.SaleTradeAmount);
    }

    public async Task<VehicleSaleDto> UpdateSaleAsync(Guid vehicleId, UpdateVehicleSaleRequest request)
    {
        if (request.SalePrice <= 0)
        {
            throw new ValidationException("Sale price must be greater than zero");
        }

        ValidateSaleTradeRequest(request.SalePrice, request.TradeAmount);
        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new NotFoundException("Vehicle not found");
        }

        if (vehicle.Status != VehicleStatus.Sold || !vehicle.ActualSalePrice.HasValue)
        {
            throw new NotFoundException("Vehicle sale record was not found");
        }

        var totalExpenseCost = await _context.VehicleExpenses
            .Where(expense => expense.VehicleId == vehicle.Id)
            .SumAsync(expense => expense.Amount);

        vehicle.ActualSalePrice = request.SalePrice;
        vehicle.SaleDate = request.SaleDate;
        ValidateTradeSelection(request.PaymentMethod, request.TradePlate, request.TradeAmount);
        vehicle.SaleTradePlate = request.PaymentMethod == PaymentMethod.Trade
            ? NormalizeOptionalText(request.TradePlate)
            : null;
        vehicle.SaleTradeAmount = request.PaymentMethod == PaymentMethod.Trade
            ? request.TradeAmount
            : null;
        vehicle.Description = string.IsNullOrWhiteSpace(request.Description)
            ? vehicle.Description
            : request.Description.Trim();

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(item =>
                item.RelatedEntityType == "VehicleSale" &&
                item.RelatedEntityId == vehicle.Id);

        if (transaction != null)
        {
            transaction.Amount = request.SalePrice;
            transaction.TransactionDate = request.SaleDate;
            transaction.Description = $"[{vehicle.Plate}] vehicle sale";
            transaction.PaymentMethod = request.PaymentMethod;
        }

        await _context.SaveChangesAsync();
        await SyncSaleReceivablePayableAsync(vehicle, request.PaymentMethod, request.CounterpartyName, request.DueDate, request.DocumentNumber, request.InstallmentCount, request.InstallmentIntervalMonths);

        var totalCost = vehicle.PurchasePrice + totalExpenseCost;
        var profit = request.SalePrice - totalCost;
        var profitMargin = totalCost == 0 ? 0 : Math.Round((profit / totalCost) * 100m, 2);

        var financeItem = await GetReceivablePayableAsync(ReceivablePayableSourceType.VehicleSale, vehicle.Id);

        return new VehicleSaleDto(
            vehicle.Id,
            vehicle.Plate,
            $"{vehicle.Brand} {vehicle.Model}",
            vehicle.PurchasePrice,
            totalExpenseCost,
            totalCost,
            request.SalePrice,
            profit,
            profitMargin,
            request.SaleDate,
            financeItem?.PaymentMethod ?? transaction?.PaymentMethod ?? PaymentMethod.Cash,
            financeItem?.CounterpartyName,
            financeItem?.DueDate,
            financeItem?.DocumentNumber,
            financeItem?.PaymentMethod == PaymentMethod.PromissoryNote ? financeItem?.OriginalAmount : null,
            null,
            null,
            vehicle.SaleTradePlate,
            vehicle.SaleTradeAmount);
    }

    public async Task<List<VehicleSaleDto>> GetSalesAsync()
    {
        var soldVehicles = await _context.Vehicles
            .Where(vehicle =>
                vehicle.OwnershipType == VehicleOwnershipType.Owned &&
                vehicle.Status == VehicleStatus.Sold &&
                vehicle.ActualSalePrice.HasValue)
            .OrderByDescending(vehicle => vehicle.UpdatedAt ?? vehicle.CreatedAt)
            .ToListAsync();

        var expenseLookup = await GetVehicleExpenseLookupAsync(soldVehicles.Select(vehicle => vehicle.Id).ToList());
        var financeLookup = await GetReceivablePayableLookupAsync(ReceivablePayableSourceType.VehicleSale, soldVehicles.Select(vehicle => vehicle.Id).ToList());
        var saleTransactionLookup = await _context.Transactions
            .Where(item => item.RelatedEntityType == "VehicleSale" && item.RelatedEntityId.HasValue)
            .ToDictionaryAsync(item => item.RelatedEntityId!.Value, item => item);

        return soldVehicles
            .Select(vehicle =>
            {
                var totalExpenseCost = expenseLookup.GetValueOrDefault(vehicle.Id);
                var totalCost = vehicle.PurchasePrice + totalExpenseCost;
                var salePrice = vehicle.ActualSalePrice ?? 0m;
                var profit = salePrice - totalCost;
                var profitMargin = totalCost == 0 ? 0 : Math.Round((profit / totalCost) * 100m, 2);
                var transaction = saleTransactionLookup.GetValueOrDefault(vehicle.Id);
                var financeItem = financeLookup.GetValueOrDefault(vehicle.Id);
                return new VehicleSaleDto(
                    vehicle.Id,
                    vehicle.Plate,
                    $"{vehicle.Brand} {vehicle.Model}",
                    vehicle.PurchasePrice,
                    totalExpenseCost,
                    totalCost,
                    salePrice,
                    profit,
                    profitMargin,
                    vehicle.SaleDate ?? vehicle.UpdatedAt ?? vehicle.CreatedAt,
                    financeItem?.PaymentMethod ?? transaction?.PaymentMethod ?? PaymentMethod.Cash,
                    financeItem?.CounterpartyName,
                    financeItem?.DueDate,
                    financeItem?.DocumentNumber,
                    financeItem?.PaymentMethod == PaymentMethod.PromissoryNote ? financeItem?.OriginalAmount : null,
                    null,
                    null,
                    vehicle.SaleTradePlate,
                    vehicle.SaleTradeAmount);
            })
            .ToList();
    }

    public async Task DeleteSaleAsync(Guid vehicleId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId);
        if (vehicle == null)
        {
            return;
        }

        vehicle.Status = VehicleStatus.InStock;
        vehicle.ActualSalePrice = null;
        vehicle.SaleDate = null;
        vehicle.SaleTradePlate = null;
        vehicle.SaleTradeAmount = null;
        await SoftDeleteReceivablePayableAsync(ReceivablePayableSourceType.VehicleSale, vehicleId);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(item =>
                item.RelatedEntityType == "VehicleSale" &&
                item.RelatedEntityId == vehicleId);

        if (transaction != null)
        {
            transaction.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        ValidateVehicleRequest(request.Plate, request.SegmentId, request.BrandId, request.ModelId, request.PurchasePrice);
        var catalog = await ResolveCatalogAsync(request.SegmentId, request.BrandId, request.ModelId);

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            throw new NotFoundException("Vehicle not found");
        }

        vehicle.Plate = request.Plate.Trim().ToUpperInvariant();
        vehicle.SegmentId = catalog.Segment.Id;
        vehicle.Segment = catalog.Segment.Name;
        vehicle.BrandId = catalog.Brand.Id;
        vehicle.Brand = catalog.Brand.Name;
        vehicle.ModelId = catalog.Model.Id;
        vehicle.Model = catalog.Model.Name;
        vehicle.Year = request.Year;
        vehicle.Color = request.Color.Trim();
        vehicle.PurchasePrice = request.PurchasePrice;
        vehicle.TargetSalePrice = request.TargetSalePrice;
        vehicle.Status = request.Status;
        vehicle.Description = request.Description;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await DeletePurchaseAsync(id);
    }

    private async Task<Dictionary<Guid, decimal>> GetVehicleExpenseLookupAsync(List<Guid> vehicleIds)
    {
        if (vehicleIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _context.VehicleExpenses
            .Where(expense => vehicleIds.Contains(expense.VehicleId))
            .GroupBy(expense => expense.VehicleId)
            .Select(group => new { VehicleId = group.Key, Total = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.VehicleId, item => item.Total);
    }

    private async Task<Dictionary<Guid, decimal?>> GetConsignmentCommissionLookupAsync(List<Vehicle> vehicles)
    {
        var consignmentIds = vehicles
            .Where(vehicle => vehicle.OwnershipType == VehicleOwnershipType.Consignment && vehicle.ConsignmentId.HasValue)
            .Select(vehicle => vehicle.ConsignmentId!.Value)
            .Distinct()
            .ToList();

        if (consignmentIds.Count == 0)
        {
            return new Dictionary<Guid, decimal?>();
        }

        var commissionByConsignmentId = await _context.Consignments
            .IgnoreQueryFilters()
            .Where(item => consignmentIds.Contains(item.Id) && !item.IsDeleted)
            .ToDictionaryAsync(item => item.Id, item => item.CommissionAmount);

        return vehicles
            .Where(vehicle => vehicle.ConsignmentId.HasValue)
            .ToDictionary(
                vehicle => vehicle.Id,
                vehicle => commissionByConsignmentId.GetValueOrDefault(vehicle.ConsignmentId!.Value));
    }

    private static VehicleDto MapVehicle(
        Vehicle vehicle,
        IReadOnlyDictionary<Guid, decimal> expenseLookup,
        IReadOnlyDictionary<Guid, decimal?> consignmentCommissionLookup)
    {
        var totalExpenseCost = expenseLookup.GetValueOrDefault(vehicle.Id);
        var totalCost = vehicle.PurchasePrice + totalExpenseCost;
        var consignmentCommissionAmount = consignmentCommissionLookup.GetValueOrDefault(vehicle.Id);
        decimal? estimatedProfit = vehicle.TargetSalePrice.HasValue
            ? vehicle.TargetSalePrice.Value - totalCost
            : null;

        return new VehicleDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.OwnershipType,
            vehicle.ConsignmentId,
            vehicle.SegmentId,
            vehicle.Segment,
            vehicle.BrandId,
            vehicle.Brand,
            vehicle.ModelId,
            vehicle.Model,
            vehicle.Year,
            vehicle.Color,
            vehicle.EngineNumber,
            vehicle.ChassisNumber,
            vehicle.PurchaseDate,
            vehicle.PurchasePrice,
            totalExpenseCost,
            totalCost,
            vehicle.TargetSalePrice,
            vehicle.ActualSalePrice,
            vehicle.SaleDate,
            consignmentCommissionAmount,
            estimatedProfit,
            vehicle.Status,
            vehicle.Description);
    }

    private static PurchaseRecordDto MapPurchaseRecord(Vehicle vehicle, ReceivablePayable? financeItem)
    {
        return new PurchaseRecordDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.OwnershipType,
            vehicle.ConsignmentId,
            vehicle.SegmentId,
            vehicle.Segment,
            vehicle.BrandId,
            vehicle.Brand,
            vehicle.ModelId,
            vehicle.Model,
            vehicle.Year,
            vehicle.Color,
            vehicle.EngineNumber,
            vehicle.ChassisNumber,
            vehicle.PurchasePrice,
            financeItem?.PaymentMethod ?? vehicle.PurchasePaymentMethod,
            vehicle.PurchaseNotaryRegistryNumber,
            financeItem?.CounterpartyName,
            financeItem?.DueDate,
            financeItem?.DocumentNumber,
            financeItem?.PaymentMethod == PaymentMethod.PromissoryNote ? financeItem?.OriginalAmount : null,
            null,
            null,
            vehicle.PurchaseTradePlate,
            vehicle.PurchaseTradeAmount,
            vehicle.TargetSalePrice,
            vehicle.PurchaseDate,
            vehicle.Description,
            vehicle.Status);
    }

    private static void ValidateVehicleRequest(string plate, Guid segmentId, Guid brandId, Guid modelId, decimal purchasePrice)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ValidationException("Plate is required");
        }

        if (segmentId == Guid.Empty || brandId == Guid.Empty || modelId == Guid.Empty)
        {
            throw new ValidationException("Segment, brand and model are required");
        }

        if (purchasePrice <= 0)
        {
            throw new ValidationException("Purchase price must be greater than zero");
        }
    }

    private static void ValidatePurchaseTradeRequest(decimal purchasePrice, decimal? tradeAmount)
    {
        if (!tradeAmount.HasValue)
        {
            return;
        }

        if (tradeAmount.Value <= 0)
        {
            throw new BusinessRuleException("Takas bedeli sifirdan buyuk olmalidir.");
        }

        if (tradeAmount.Value > purchasePrice)
        {
            throw new BusinessRuleException("Takas bedeli toplam alim tutarindan buyuk olamaz.");
        }
    }

    private static void ValidateSaleTradeRequest(decimal salePrice, decimal? tradeAmount)
    {
        if (!tradeAmount.HasValue)
        {
            return;
        }

        if (tradeAmount.Value <= 0)
        {
            throw new BusinessRuleException("Takas bedeli sifirdan buyuk olmalidir.");
        }

        if (tradeAmount.Value > salePrice)
        {
            throw new BusinessRuleException("Takas bedeli toplam satis tutarindan buyuk olamaz.");
        }
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
            throw new NotFoundException("Selected segment, brand or model was not found");
        }

        if (model.VehicleBrandId != brand.Id)
        {
            throw new ValidationException("Selected model does not belong to the selected brand");
        }

        if (model.VehicleSegmentId.HasValue && model.VehicleSegmentId != segment.Id)
        {
            throw new ValidationException("Selected model does not match the selected segment");
        }

        return (segment, brand, model);
    }

    private async Task EnsureVehicleLookupsAsync()
    {
        if (await _context.VehicleSegments.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            return;
        }

        var segments = new[]
        {
            "Sedan",
            "SUV",
            "Hatchback",
            "Coupe",
            "Pickup",
            "Van"
        };

        var brands = new[]
        {
            "BMW",
            "Mercedes-Benz",
            "Audi",
            "Volkswagen",
            "Renault",
            "Fiat",
            "Ford",
            "Toyota"
        };

        var segmentEntities = segments.Select(name => new VehicleSegment
        {
            TenantId = SharedLookupTenantId,
            Name = name
        }).ToList();

        var brandEntities = brands.Select(name => new VehicleBrand
        {
            TenantId = SharedLookupTenantId,
            Name = name
        }).ToList();

        _context.VehicleSegments.AddRange(segmentEntities);
        _context.VehicleBrands.AddRange(brandEntities);
        await _context.SaveChangesAsync();

        var segmentMap = segmentEntities.ToDictionary(item => item.Name);
        var brandMap = brandEntities.ToDictionary(item => item.Name);

        var models = new (string Name, string Brand, string Segment)[]
        {
            ("320i", "BMW", "Sedan"),
            ("X5", "BMW", "SUV"),
            ("C180", "Mercedes-Benz", "Sedan"),
            ("GLC", "Mercedes-Benz", "SUV"),
            ("A4", "Audi", "Sedan"),
            ("Q5", "Audi", "SUV"),
            ("Passat", "Volkswagen", "Sedan"),
            ("Tiguan", "Volkswagen", "SUV"),
            ("Clio", "Renault", "Hatchback"),
            ("Megane", "Renault", "Sedan"),
            ("Egea", "Fiat", "Sedan"),
            ("Doblo", "Fiat", "Van"),
            ("Focus", "Ford", "Sedan"),
            ("Ranger", "Ford", "Pickup"),
            ("Corolla", "Toyota", "Sedan"),
            ("C-HR", "Toyota", "SUV")
        };

        _context.VehicleCatalogModels.AddRange(models.Select(item => new VehicleCatalogModel
        {
            TenantId = SharedLookupTenantId,
            Name = item.Name,
            VehicleBrandId = brandMap[item.Brand].Id,
            VehicleSegmentId = segmentMap[item.Segment].Id
        }));

        await _context.SaveChangesAsync();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToUpperInvariant();
    }

    private async Task SyncPurchaseReceivablePayableAsync(
        Vehicle vehicle,
        PaymentMethod paymentMethod,
        string? counterpartyName,
        DateTime? dueDate,
        string? documentNumber,
        int? installmentCount,
        int? installmentIntervalMonths)
    {
        await SyncReceivablePayableAsync(
            ReceivablePayableType.Payable,
            ReceivablePayableSourceType.VehiclePurchase,
            vehicle.Id,
            vehicle.PurchasePrice,
            vehicle.PurchaseDate,
            paymentMethod,
            counterpartyName,
            dueDate,
            documentNumber,
            installmentCount,
            installmentIntervalMonths,
            $"[{vehicle.Plate}] arac alimi");
    }

    private async Task SyncSaleReceivablePayableAsync(
        Vehicle vehicle,
        PaymentMethod paymentMethod,
        string? counterpartyName,
        DateTime? dueDate,
        string? documentNumber,
        int? installmentCount,
        int? installmentIntervalMonths)
    {
        await SyncReceivablePayableAsync(
            ReceivablePayableType.Receivable,
            ReceivablePayableSourceType.VehicleSale,
            vehicle.Id,
            vehicle.ActualSalePrice ?? 0m,
            vehicle.SaleDate ?? vehicle.UpdatedAt ?? vehicle.CreatedAt,
            paymentMethod,
            counterpartyName,
            dueDate,
            documentNumber,
            installmentCount,
            installmentIntervalMonths,
            $"[{vehicle.Plate}] arac satisi");
    }

    private async Task SyncReceivablePayableAsync(
        ReceivablePayableType type,
        ReceivablePayableSourceType sourceType,
        Guid sourceId,
        decimal amount,
        DateTime issueDate,
        PaymentMethod paymentMethod,
        string? counterpartyName,
        DateTime? dueDate,
        string? documentNumber,
        int? installmentCount,
        int? installmentIntervalMonths,
        string description)
    {
        var existing = await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .Where(item => item.SourceType == sourceType && item.SourceId == sourceId && !item.IsDeleted)
            .OrderBy(item => item.DueDate)
            .ToListAsync();

        if (!RequiresReceivablePayable(paymentMethod))
        {
            foreach (var item in existing)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            throw new BusinessRuleException("Vadeli, cek veya senet islemlerinde taraf bilgisi zorunludur.");
        }

        if (!dueDate.HasValue)
        {
            throw new BusinessRuleException("Vadeli, cek veya senet islemlerinde vade tarihi zorunludur.");
        }

        var normalizedCounterparty = counterpartyName.Trim();
        var normalizedDocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim().ToUpperInvariant();
        var documentType = GetFinancialDocumentType(paymentMethod);
        var status = dueDate.Value.Date < DateTime.UtcNow.Date ? ReceivablePayableStatus.Overdue : ReceivablePayableStatus.Open;

        foreach (var item in existing)
        {
            item.IsDeleted = true;
        }

        if (paymentMethod == PaymentMethod.PromissoryNote)
        {
            if (!installmentCount.HasValue || installmentCount.Value <= 0)
            {
                throw new BusinessRuleException("Senet islemlerinde taksit sayisi zorunludur.");
            }

            if (!installmentIntervalMonths.HasValue || installmentIntervalMonths.Value <= 0)
            {
                throw new BusinessRuleException("Senet islemlerinde taksit araligi zorunludur.");
            }

            var calculatedInstallmentAmount = Math.Round(amount / installmentCount.Value, 2, MidpointRounding.AwayFromZero);
            var currentDueDate = dueDate.Value;
            var remainingAmount = amount;
            var installmentIndex = 1;
            var totalInstallments = installmentCount.Value;

            while (installmentIndex <= totalInstallments && remainingAmount > 0)
            {
                var currentAmount = installmentIndex == totalInstallments
                    ? remainingAmount
                    : Math.Min(calculatedInstallmentAmount, remainingAmount);
                var currentStatus = currentDueDate.Date < DateTime.UtcNow.Date ? ReceivablePayableStatus.Overdue : ReceivablePayableStatus.Open;

                _context.ReceivablePayables.Add(new ReceivablePayable
                {
                    Type = type,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    CounterpartyName = normalizedCounterparty,
                    PaymentMethod = paymentMethod,
                    DocumentType = documentType,
                    DocumentNumber = normalizedDocumentNumber,
                    IssueDate = issueDate,
                    DueDate = currentDueDate,
                    OriginalAmount = currentAmount,
                    RemainingAmount = currentAmount,
                    Status = currentStatus,
                    Description = $"{description} / Senet {installmentIndex}/{totalInstallments}"
                });

                remainingAmount -= currentAmount;
                currentDueDate = currentDueDate.AddMonths(installmentIntervalMonths.Value);
                installmentIndex++;
            }
        }
        else
        {
            _context.ReceivablePayables.Add(new ReceivablePayable
            {
                Type = type,
                SourceType = sourceType,
                SourceId = sourceId,
                CounterpartyName = normalizedCounterparty,
                PaymentMethod = paymentMethod,
                DocumentType = documentType,
                DocumentNumber = normalizedDocumentNumber,
                IssueDate = issueDate,
                DueDate = dueDate.Value,
                OriginalAmount = amount,
                RemainingAmount = amount,
                Status = status,
                Description = description
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SoftDeleteReceivablePayableAsync(ReceivablePayableSourceType sourceType, Guid sourceId)
    {
        var existing = await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .Where(item => item.SourceType == sourceType && item.SourceId == sourceId && !item.IsDeleted)
            .ToListAsync();

        foreach (var item in existing)
        {
            item.IsDeleted = true;
        }
    }

    private async Task<Dictionary<Guid, ReceivablePayable>> GetReceivablePayableLookupAsync(
        ReceivablePayableSourceType sourceType,
        List<Guid> sourceIds)
    {
        if (sourceIds.Count == 0)
        {
            return new Dictionary<Guid, ReceivablePayable>();
        }

        var items = await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .Where(item => item.SourceType == sourceType && sourceIds.Contains(item.SourceId) && !item.IsDeleted)
            .OrderBy(item => item.DueDate)
            .ToListAsync();

        return items
            .GroupBy(item => item.SourceId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<ReceivablePayable?> GetReceivablePayableAsync(
        ReceivablePayableSourceType sourceType,
        Guid sourceId)
    {
        return await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .Where(item => item.SourceType == sourceType && item.SourceId == sourceId && !item.IsDeleted)
            .OrderBy(item => item.DueDate)
            .FirstOrDefaultAsync();
    }

    private static bool RequiresReceivablePayable(PaymentMethod paymentMethod) =>
        paymentMethod is PaymentMethod.Check or PaymentMethod.PromissoryNote or PaymentMethod.Deferred;

    private static FinancialDocumentType GetFinancialDocumentType(PaymentMethod paymentMethod) =>
        paymentMethod switch
        {
            PaymentMethod.Check => FinancialDocumentType.Check,
            PaymentMethod.PromissoryNote => FinancialDocumentType.PromissoryNote,
            _ => FinancialDocumentType.OpenAccount
        };

    private static void ValidateTradeSelection(PaymentMethod paymentMethod, string? tradePlate, decimal? tradeAmount)
    {
        if (paymentMethod != PaymentMethod.Trade)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tradePlate))
        {
            throw new BusinessRuleException("Takas secildiginde takas aracinin plakasini girmelisiniz.");
        }

        if (!tradeAmount.HasValue || tradeAmount.Value <= 0)
        {
            throw new BusinessRuleException("Takas secildiginde takas bedeli sifirdan buyuk olmalidir.");
        }
    }
}
