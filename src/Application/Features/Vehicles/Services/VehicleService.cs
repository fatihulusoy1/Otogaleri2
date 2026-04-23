using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Vehicles.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public class VehicleService : IVehicleService
{
    private readonly IApplicationDbContext _context;

    public VehicleService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleDto>> GetAllAsync()
    {
        var vehicles = await _context.Vehicles
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .ToListAsync();

        var expenseLookup = await GetVehicleExpenseLookupAsync(vehicles.Select(vehicle => vehicle.Id).ToList());
        return vehicles.Select(vehicle => MapVehicle(vehicle, expenseLookup)).ToList();
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return null;
        }

        var expenseLookup = await GetVehicleExpenseLookupAsync(new List<Guid> { vehicle.Id });
        return MapVehicle(vehicle, expenseLookup);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        ValidateVehicleRequest(request.Plate, request.Brand, request.Model, request.PurchasePrice);

        var vehicle = new Vehicle
        {
            Plate = request.Plate.Trim().ToUpperInvariant(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Color = request.Color.Trim(),
            EngineNumber = request.EngineNumber.Trim(),
            ChassisNumber = request.ChassisNumber.Trim(),
            PurchasePrice = request.PurchasePrice,
            TargetSalePrice = request.TargetSalePrice,
            Status = VehicleStatus.InStock,
            Description = request.Description
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return MapVehicle(vehicle, new Dictionary<Guid, decimal> { [vehicle.Id] = 0m });
    }

    public async Task<PurchaseRecordDto> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        var vehicle = await CreateAsync(new CreateVehicleRequest(
            request.Plate,
            request.Brand,
            request.Model,
            request.Year,
            request.Color,
            request.EngineNumber,
            request.ChassisNumber,
            request.PurchasePrice,
            request.TargetSalePrice,
            request.Description));

        return new PurchaseRecordDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.PurchasePrice,
            DateTime.UtcNow,
            vehicle.Status);
    }

    public async Task<List<PurchaseRecordDto>> GetPurchasesAsync()
    {
        return await _context.Vehicles
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .Select(vehicle => new PurchaseRecordDto(
                vehicle.Id,
                vehicle.Plate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.PurchasePrice,
                vehicle.CreatedAt,
                vehicle.Status))
            .ToListAsync();
    }

    public async Task<VehicleExpenseDto> AddExpenseAsync(Guid vehicleId, CreateVehicleExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new Exception("Expense amount must be greater than zero");
        }

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new Exception("Vehicle not found");
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
            .ToDictionaryAsync(transaction => transaction.RelatedEntityId ?? Guid.Empty, transaction => transaction.PaymentMethod);

        var expenses = await expensesQuery
            .OrderByDescending(item => item.expense.ExpenseDate)
            .Select(item => new VehicleExpenseDto(
                item.expense.Id,
                item.expense.VehicleId,
                item.vehicle.Plate,
                item.expense.Description,
                item.expense.Amount,
                item.expense.ExpenseDate,
                PaymentMethod.Cash))
            .ToListAsync();

        return expenses
            .Select(expense => expense with
            {
                PaymentMethod = paymentLookup.GetValueOrDefault(expense.Id, PaymentMethod.Cash)
            })
            .ToList();
    }

    public async Task<VehicleSaleDto> CompleteSaleAsync(Guid vehicleId, CompleteVehicleSaleRequest request)
    {
        if (request.SalePrice <= 0)
        {
            throw new Exception("Sale price must be greater than zero");
        }

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            throw new Exception("Vehicle not found");
        }

        if (vehicle.Status == VehicleStatus.Sold)
        {
            throw new Exception("Vehicle is already sold");
        }

        var totalExpenseCost = await _context.VehicleExpenses
            .Where(expense => expense.VehicleId == vehicle.Id)
            .SumAsync(expense => expense.Amount);

        vehicle.Status = VehicleStatus.Sold;
        vehicle.ActualSalePrice = request.SalePrice;
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

        var totalCost = vehicle.PurchasePrice + totalExpenseCost;
        var profit = request.SalePrice - totalCost;
        var profitMargin = request.SalePrice == 0 ? 0 : Math.Round((profit / request.SalePrice) * 100m, 2);

        return new VehicleSaleDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.PurchasePrice,
            totalExpenseCost,
            totalCost,
            request.SalePrice,
            profit,
            profitMargin,
            request.SaleDate);
    }

    public async Task<List<VehicleSaleDto>> GetSalesAsync()
    {
        var soldVehicles = await _context.Vehicles
            .Where(vehicle => vehicle.Status == VehicleStatus.Sold && vehicle.ActualSalePrice.HasValue)
            .OrderByDescending(vehicle => vehicle.UpdatedAt ?? vehicle.CreatedAt)
            .ToListAsync();

        var expenseLookup = await GetVehicleExpenseLookupAsync(soldVehicles.Select(vehicle => vehicle.Id).ToList());

        return soldVehicles
            .Select(vehicle =>
            {
                var totalExpenseCost = expenseLookup.GetValueOrDefault(vehicle.Id);
                var totalCost = vehicle.PurchasePrice + totalExpenseCost;
                var salePrice = vehicle.ActualSalePrice ?? 0m;
                var profit = salePrice - totalCost;
                var profitMargin = salePrice == 0 ? 0 : Math.Round((profit / salePrice) * 100m, 2);

                return new VehicleSaleDto(
                    vehicle.Id,
                    vehicle.Plate,
                    vehicle.PurchasePrice,
                    totalExpenseCost,
                    totalCost,
                    salePrice,
                    profit,
                    profitMargin,
                    vehicle.UpdatedAt ?? vehicle.CreatedAt);
            })
            .ToList();
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        ValidateVehicleRequest(request.Plate, request.Brand, request.Model, request.PurchasePrice);

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            throw new Exception("Vehicle not found");
        }

        vehicle.Plate = request.Plate.Trim().ToUpperInvariant();
        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
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
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return;
        }

        vehicle.IsDeleted = true;
        await _context.SaveChangesAsync();
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

    private static VehicleDto MapVehicle(Vehicle vehicle, IReadOnlyDictionary<Guid, decimal> expenseLookup)
    {
        var totalExpenseCost = expenseLookup.GetValueOrDefault(vehicle.Id);
        var totalCost = vehicle.PurchasePrice + totalExpenseCost;
        decimal? estimatedProfit = vehicle.TargetSalePrice.HasValue
            ? vehicle.TargetSalePrice.Value - totalCost
            : null;

        return new VehicleDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.Color,
            vehicle.PurchasePrice,
            totalExpenseCost,
            totalCost,
            vehicle.TargetSalePrice,
            vehicle.ActualSalePrice,
            estimatedProfit,
            vehicle.Status,
            vehicle.Description);
    }

    private static void ValidateVehicleRequest(string plate, string brand, string model, decimal purchasePrice)
    {
        if (string.IsNullOrWhiteSpace(plate) || string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model))
        {
            throw new Exception("Plate, brand and model are required");
        }

        if (purchasePrice <= 0)
        {
            throw new Exception("Purchase price must be greater than zero");
        }
    }
}
