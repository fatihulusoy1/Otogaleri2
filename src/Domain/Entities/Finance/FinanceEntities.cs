using AutoGallerySaaS.Domain.Common;

namespace AutoGallerySaaS.Domain.Entities.Finance;

public class IncomeCategory : BaseTenantEntity
{
    public string Name { get; set; } = null!;
}

public class ExpenseCategory : BaseTenantEntity
{
    public string Name { get; set; } = null!;
    public ExpenseCategoryType CategoryType { get; set; } = ExpenseCategoryType.Vehicle;
}

public class VehicleExpenseCategory : BaseTenantEntity
{
    public string Name { get; set; } = null!;
}

public class GeneralExpenseCategory : BaseTenantEntity
{
    public string Name { get; set; } = null!;
}

public class Transaction : BaseTenantEntity
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Guid? RelatedEntityId { get; set; } // CustomerId or SupplierId
    public string? RelatedEntityType { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
}

public class ReceivablePayable : BaseTenantEntity
{
    public ReceivablePayableType Type { get; set; }
    public ReceivablePayableSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public string CounterpartyName { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; }
    public FinancialDocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? LastSettlementDate { get; set; }
    public ReceivablePayableStatus Status { get; set; } = ReceivablePayableStatus.Open;
    public string? Description { get; set; }
}

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    CreditCard = 3,
    Trade = 4,
    Check = 5,
    PromissoryNote = 6,
    Deferred = 7
}

public enum ReceivablePayableType
{
    Receivable = 1,
    Payable = 2
}

public enum ReceivablePayableSourceType
{
    VehicleSale = 1,
    VehiclePurchase = 2
}

public enum FinancialDocumentType
{
    OpenAccount = 1,
    Check = 2,
    PromissoryNote = 3
}

public enum ReceivablePayableStatus
{
    Open = 1,
    PartiallyPaid = 2,
    Closed = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum ExpenseCategoryType
{
    Vehicle = 1,
    General = 2
}
