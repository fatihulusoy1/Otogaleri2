using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
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
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionDto(t.Id, t.Type, t.Amount, t.TransactionDate, t.Description, t.PaymentMethod))
            .ToListAsync();
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            Type = request.Type,
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Description = request.Description,
            PaymentMethod = request.PaymentMethod,
            CategoryId = request.CategoryId
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new TransactionDto(transaction.Id, transaction.Type, transaction.Amount, transaction.TransactionDate, transaction.Description, transaction.PaymentMethod);
    }
}
