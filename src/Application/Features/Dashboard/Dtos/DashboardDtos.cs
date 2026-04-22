using AutoGallerySaaS.Application.Features.Auth.Dtos;
namespace AutoGallerySaaS.Application.Features.Dashboard.Dtos;

public record DashboardSummaryDto(
    int TotalVehicles,
    int InStockVehicles,
    decimal TotalStockValue,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    decimal NetProfit
);
