using AutoGallerySaaS.Application.Features.Consignments.Dtos;

namespace AutoGallerySaaS.Application.Features.Consignments.Services;

public interface IConsignmentService
{
    Task<List<BrokeredConsignmentDto>> GetBrokeredAsync();
    Task<BrokeredConsignmentDto> CreateBrokeredAsync(CreateBrokeredConsignmentRequest request);
    Task<BrokeredConsignmentDto> UpdateBrokeredAsync(Guid id, UpdateBrokeredConsignmentRequest request);
    Task DeleteBrokeredAsync(Guid id);

    Task<List<StockConsignmentDto>> GetStockAsync();
    Task<StockConsignmentDto> CreateStockAsync(CreateStockConsignmentRequest request);
    Task<StockConsignmentDto> UpdateStockAsync(Guid id, UpdateStockConsignmentRequest request);
    Task<StockConsignmentDto> CompleteStockSaleAsync(Guid id, CompleteStockConsignmentSaleRequest request);
    Task<StockConsignmentDto> UpdateStockSaleAsync(Guid id, CompleteStockConsignmentSaleRequest request);
    Task DeleteStockAsync(Guid id);
    Task DeleteStockSaleAsync(Guid id);
}
