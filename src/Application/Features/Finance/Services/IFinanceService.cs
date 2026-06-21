using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Finance.Services;

public interface IFinanceService
{
    Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync(ExpenseCategoryType? categoryType = null);
    Task<List<TransactionDto>> GetTransactionsAsync();
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request);
    Task<TransactionDto> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequest request);
    Task DeleteTransactionAsync(Guid transactionId);
    Task<List<ReceivablePayableDto>> GetReceivablePayablesAsync(ReceivablePayableType type);
    Task<ReceivablePayableDto> SettleReceivablePayableAsync(Guid id, DateTime settlementDate);
    Task<ReceivablePayableDto> ApplyPartialSettlementAsync(Guid id, decimal amount, DateTime settlementDate);
    Task<ReceivablePayableDto> MarkReceivablePayableOverdueAsync(Guid id);
    Task<ReceivablePayableDto> ReopenReceivablePayableAsync(Guid id);
}
