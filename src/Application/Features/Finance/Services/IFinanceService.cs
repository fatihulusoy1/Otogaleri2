using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Dtos;

namespace AutoGallerySaaS.Application.Features.Finance.Services;

public interface IFinanceService
{
    Task<List<TransactionDto>> GetTransactionsAsync();
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request);
}
