using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Dashboard.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var vehicles = await _context.Vehicles.ToListAsync();
        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();

        var expenseLookup = vehicleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _context.VehicleExpenses
                .Where(expense => vehicleIds.Contains(expense.VehicleId))
                .GroupBy(expense => expense.VehicleId)
                .Select(group => new { VehicleId = group.Key, Total = group.Sum(item => item.Amount) })
                .ToDictionaryAsync(item => item.VehicleId, item => item.Total);

        var totalVehicles = vehicles.Count;
        var inStockVehicles = vehicles.Count(vehicle => vehicle.Status == VehicleStatus.InStock);
        var soldVehicles = vehicles.Count(vehicle => vehicle.Status == VehicleStatus.Sold);

        var totalPurchaseCost = vehicles.Sum(vehicle => vehicle.PurchasePrice);
        var totalVehicleExpenseCost = expenseLookup.Values.Sum();
        var totalCostWithExpenses = totalPurchaseCost + totalVehicleExpenseCost;

        var soldVehicleList = vehicles
            .Where(vehicle => vehicle.Status == VehicleStatus.Sold && vehicle.ActualSalePrice.HasValue)
            .ToList();

        var totalSalesRevenue = soldVehicleList.Sum(vehicle => vehicle.ActualSalePrice ?? 0m);
        var soldVehicleCost = soldVehicleList.Sum(vehicle => vehicle.PurchasePrice + expenseLookup.GetValueOrDefault(vehicle.Id));
        var grossProfit = totalSalesRevenue - soldVehicleCost;
        var grossProfitMargin = totalSalesRevenue == 0 ? 0 : Math.Round((grossProfit / totalSalesRevenue) * 100m, 2);

        var currentStockVehicles = vehicles.Where(vehicle => vehicle.Status == VehicleStatus.InStock).ToList();
        var currentStockPurchaseCost = currentStockVehicles.Sum(vehicle => vehicle.PurchasePrice);
        var currentStockExpenseCost = currentStockVehicles.Sum(vehicle => expenseLookup.GetValueOrDefault(vehicle.Id));
        var currentStockTotalCost = currentStockPurchaseCost + currentStockExpenseCost;

        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var currentMonthPurchaseCost = vehicles
            .Where(vehicle => vehicle.CreatedAt >= firstDayOfMonth)
            .Sum(vehicle => vehicle.PurchasePrice);

        var currentMonthExpenseCost = await _context.Transactions
            .Where(transaction => transaction.Type == TransactionType.Expense && transaction.TransactionDate >= firstDayOfMonth)
            .SumAsync(transaction => transaction.Amount);

        var currentMonthSalesRevenue = await _context.Transactions
            .Where(transaction => transaction.Type == TransactionType.Income && transaction.TransactionDate >= firstDayOfMonth)
            .SumAsync(transaction => transaction.Amount);

        return new DashboardSummaryDto(
            totalVehicles,
            inStockVehicles,
            soldVehicles,
            totalPurchaseCost,
            totalVehicleExpenseCost,
            totalCostWithExpenses,
            totalSalesRevenue,
            grossProfit,
            grossProfitMargin,
            currentStockPurchaseCost,
            currentStockExpenseCost,
            currentStockTotalCost,
            currentMonthPurchaseCost,
            currentMonthExpenseCost,
            currentMonthSalesRevenue);
    }
}
