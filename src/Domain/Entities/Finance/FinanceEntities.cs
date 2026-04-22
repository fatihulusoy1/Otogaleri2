using AutoGallerySaaS.Domain.Common;

namespace AutoGallerySaaS.Domain.Entities.Finance;

public class IncomeCategory : BaseTenantEntity
{
    public string Name { get; set; } = null!;
}

public class ExpenseCategory : BaseTenantEntity
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

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    CreditCard = 3
}
