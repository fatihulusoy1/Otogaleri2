namespace AutoGallerySaaS.Application.Features.Dashboard.Dtos;

public record DashboardSummaryDto(
    int TotalVehicles,
    int InStockVehicles,
    int SoldVehicles,
    decimal TotalPurchaseCost,
    decimal TotalVehicleExpenseCost,
    decimal TotalCostWithExpenses,
    decimal TotalSalesRevenue,
    decimal GrossProfit,
    decimal GrossProfitMargin,
    decimal CurrentStockPurchaseCost,
    decimal CurrentStockExpenseCost,
    decimal CurrentStockTotalCost,
    decimal CurrentMonthPurchaseCost,
    decimal CurrentMonthExpenseCost,
    decimal CurrentMonthSalesRevenue
);
