using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
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
        var totalVehicles = await _context.Vehicles.CountAsync();
        var inStockVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.InStock);
        var totalStockValue = await _context.Vehicles.Where(v => v.Status == VehicleStatus.InStock).SumAsync(v => v.PurchasePrice);

        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var monthlyIncome = await _context.Transactions
            .Where(t => t.Type == TransactionType.Income && t.TransactionDate >= firstDayOfMonth)
            .SumAsync(t => t.Amount);

        var monthlyExpense = await _context.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.TransactionDate >= firstDayOfMonth)
            .SumAsync(t => t.Amount);

        return new DashboardSummaryDto(
            totalVehicles,
            inStockVehicles,
            totalStockValue,
            monthlyIncome,
            monthlyExpense,
            monthlyIncome - monthlyExpense
        );
    }
}
