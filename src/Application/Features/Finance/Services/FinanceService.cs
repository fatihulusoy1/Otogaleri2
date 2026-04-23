using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Finance.Services;

public class FinanceService : IFinanceService
{
    private readonly IApplicationDbContext _context;

    public FinanceService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        return await _context.Transactions
            .OrderByDescending(transaction => transaction.TransactionDate)
            .Select(transaction => new TransactionDto(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.TransactionDate,
                transaction.Description,
                transaction.PaymentMethod,
                transaction.CategoryId,
                transaction.RelatedEntityId,
                transaction.RelatedEntityType))
            .ToListAsync();
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new Exception("Transaction amount must be greater than zero");
        }

        var transaction = new Transaction
        {
            Type = request.Type,
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Description = request.Description.Trim(),
            PaymentMethod = request.PaymentMethod,
            CategoryId = request.CategoryId,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.PaymentMethod,
            transaction.CategoryId,
            transaction.RelatedEntityId,
            transaction.RelatedEntityType);
    }
}
