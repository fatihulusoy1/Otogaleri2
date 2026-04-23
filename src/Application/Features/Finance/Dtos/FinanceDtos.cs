using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Finance.Dtos;

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    Guid? RelatedEntityId,
    string? RelatedEntityType
);

public record CreateTransactionRequest(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    Guid? RelatedEntityId,
    string? RelatedEntityType
);
