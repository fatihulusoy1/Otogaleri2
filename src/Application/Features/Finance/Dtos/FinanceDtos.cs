using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Finance.Dtos;

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    PaymentMethod PaymentMethod
);

public record CreateTransactionRequest(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    PaymentMethod PaymentMethod,
    Guid? CategoryId
);
