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
    string? CategoryName,
    Guid? RelatedEntityId,
    string? RelatedEntityType
);

public record ExpenseCategoryDto(
    Guid Id,
    string Name,
    ExpenseCategoryType CategoryType
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

public record UpdateTransactionRequest(
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    Guid? RelatedEntityId,
    string? RelatedEntityType
);

public record ReceivablePayableDto(
    Guid Id,
    ReceivablePayableType Type,
    ReceivablePayableSourceType SourceType,
    Guid SourceId,
    string? Plate,
    string CounterpartyName,
    PaymentMethod PaymentMethod,
    FinancialDocumentType DocumentType,
    string? DocumentNumber,
    DateTime IssueDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal RemainingAmount,
    DateTime? LastSettlementDate,
    ReceivablePayableStatus Status,
    string? Description
);

public record PartialSettlementRequest(
    decimal Amount,
    DateTime SettlementDate
);

public record SettleReceivablePayableRequest(
    DateTime SettlementDate
);
