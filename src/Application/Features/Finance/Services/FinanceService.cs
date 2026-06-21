using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Finance.Services;

public class FinanceService : IFinanceService
{
    private readonly IApplicationDbContext _context;

    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public FinanceService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync(ExpenseCategoryType? categoryType = null)
    {
        if (categoryType == ExpenseCategoryType.Vehicle)
        {
            return await _context.VehicleExpenseCategories
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.Vehicle))
                .ToListAsync();
        }

        if (categoryType == ExpenseCategoryType.General)
        {
            return await _context.GeneralExpenseCategories
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.General))
                .ToListAsync();
        }

        var vehicleCategories = await _context.VehicleExpenseCategories
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.Vehicle))
            .ToListAsync();

        var generalCategories = await _context.GeneralExpenseCategories
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.General))
            .ToListAsync();

        return vehicleCategories.Concat(generalCategories).OrderBy(item => item.Name).ToList();
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        var currentTenantId = _context.CurrentTenantId ?? Guid.Empty;

        return await _context.Transactions
            .IgnoreQueryFilters()
            .Where(transaction =>
                !transaction.IsDeleted &&
                (_context.CurrentUserIsSuperAdmin || transaction.TenantId == currentTenantId))
            .OrderByDescending(transaction => transaction.TransactionDate)
            .Select(transaction => new TransactionDto(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.TransactionDate,
                transaction.Description,
                transaction.PaymentMethod,
                transaction.CategoryId,
                transaction.CategoryId != null
                    ? transaction.RelatedEntityType == "GeneralExpense"
                        ? _context.GeneralExpenseCategories.IgnoreQueryFilters()
                            .Where(category => category.Id == transaction.CategoryId && category.TenantId == SharedLookupTenantId && !category.IsDeleted)
                            .Select(category => category.Name)
                            .FirstOrDefault()
                        : _context.VehicleExpenseCategories.IgnoreQueryFilters()
                            .Where(category => category.Id == transaction.CategoryId && category.TenantId == SharedLookupTenantId && !category.IsDeleted)
                            .Select(category => category.Name)
                            .FirstOrDefault()
                    : null,
                transaction.RelatedEntityId,
                transaction.RelatedEntityType))
            .ToListAsync();
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request)
    {
        await ValidateTransactionRequestAsync(
            request.Type,
            request.Amount,
            request.CategoryId,
            request.RelatedEntityType);

        var currentTenantId = _context.CurrentTenantId;
        if (!currentTenantId.HasValue || currentTenantId == Guid.Empty)
        {
            throw new BusinessRuleException("Tenant bilgisi bulunamadi.");
        }

        var transaction = new Transaction
        {
            TenantId = currentTenantId.Value,
            Type = request.Type,
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Description = request.Description?.Trim() ?? string.Empty,
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
            null,
            transaction.RelatedEntityId,
            transaction.RelatedEntityType);
    }

    public async Task<TransactionDto> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequest request)
    {
        await ValidateTransactionRequestAsync(
            request.Type,
            request.Amount,
            request.CategoryId,
            request.RelatedEntityType);

        var transaction = await GetAccessibleTransactionAsync(transactionId);

        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.TransactionDate = request.TransactionDate;
        transaction.Description = request.Description?.Trim() ?? string.Empty;
        transaction.PaymentMethod = request.PaymentMethod;
        transaction.CategoryId = request.CategoryId;
        transaction.RelatedEntityId = request.RelatedEntityId;
        transaction.RelatedEntityType = request.RelatedEntityType;

        await _context.SaveChangesAsync();

        var categoryName = await ResolveCategoryNameAsync(transaction.CategoryId, transaction.RelatedEntityType);

        return new TransactionDto(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Description,
            transaction.PaymentMethod,
            transaction.CategoryId,
            categoryName,
            transaction.RelatedEntityId,
            transaction.RelatedEntityType);
    }

    public async Task DeleteTransactionAsync(Guid transactionId)
    {
        var transaction = await GetAccessibleTransactionAsync(transactionId);

        transaction.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<ReceivablePayableDto>> GetReceivablePayablesAsync(ReceivablePayableType type)
    {
        var currentTenantId = _context.CurrentTenantId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var items = await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .Where(item =>
                !item.IsDeleted &&
                item.Type == type &&
                (_context.CurrentUserIsSuperAdmin || item.TenantId == currentTenantId))
            .OrderBy(item => item.Status == ReceivablePayableStatus.Open ? 0 : 1)
            .ThenBy(item => item.DueDate)
            .ToListAsync();

        var vehicleSourceIds = items
            .Where(item =>
                item.SourceType == ReceivablePayableSourceType.VehiclePurchase ||
                item.SourceType == ReceivablePayableSourceType.VehicleSale)
            .Select(item => item.SourceId)
            .Distinct()
            .ToList();

        var plateLookup = vehicleSourceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Vehicles
                .IgnoreQueryFilters()
                .Where(vehicle => vehicleSourceIds.Contains(vehicle.Id))
                .Select(vehicle => new { vehicle.Id, vehicle.Plate })
                .ToDictionaryAsync(vehicle => vehicle.Id, vehicle => vehicle.Plate);

        return items.Select(item =>
        {
            var status = item.Status;
            if (status == ReceivablePayableStatus.Open && item.DueDate.Date < now.Date)
            {
                status = ReceivablePayableStatus.Overdue;
            }

            plateLookup.TryGetValue(item.SourceId, out var plate);
            return MapReceivablePayable(item, status, plate);
        }).ToList();
    }

    public async Task<ReceivablePayableDto> SettleReceivablePayableAsync(Guid id, DateTime settlementDate)
    {
        var item = await GetAccessibleReceivablePayableAsync(id);
        item.RemainingAmount = 0;
        item.LastSettlementDate = settlementDate;
        item.Status = ReceivablePayableStatus.Closed;
        await _context.SaveChangesAsync();
        return await MapReceivablePayableAsync(item, item.Status);
    }

    public async Task<ReceivablePayableDto> ApplyPartialSettlementAsync(Guid id, decimal amount, DateTime settlementDate)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Tahsil edilen / odenen tutar sifirdan buyuk olmalidir.");
        }

        var item = await GetAccessibleReceivablePayableAsync(id);
        if (item.Status == ReceivablePayableStatus.Closed)
        {
            throw new BusinessRuleException("Kapali kayda kismi odeme uygulanamaz.");
        }

        if (amount > item.RemainingAmount)
        {
            throw new ValidationException("Girilen tutar kalan borc / alacak tutarindan buyuk olamaz.");
        }

        if (amount >= item.RemainingAmount)
        {
            item.RemainingAmount = 0;
            item.LastSettlementDate = settlementDate;
            item.Status = ReceivablePayableStatus.Closed;
        }
        else
        {
            item.RemainingAmount -= amount;
            item.LastSettlementDate = settlementDate;
            item.Status = ReceivablePayableStatus.PartiallyPaid;
        }

        await _context.SaveChangesAsync();
        return await MapReceivablePayableAsync(item, item.Status);
    }

    public async Task<ReceivablePayableDto> MarkReceivablePayableOverdueAsync(Guid id)
    {
        var item = await GetAccessibleReceivablePayableAsync(id);
        if (item.Status == ReceivablePayableStatus.Closed)
        {
            throw new BusinessRuleException("Kapali kayit gecikmis olarak isaretlenemez.");
        }

        item.Status = ReceivablePayableStatus.Overdue;
        await _context.SaveChangesAsync();
        return await MapReceivablePayableAsync(item, item.Status);
    }

    public async Task<ReceivablePayableDto> ReopenReceivablePayableAsync(Guid id)
    {
        var item = await GetAccessibleReceivablePayableAsync(id);
        if (item.Status != ReceivablePayableStatus.Closed)
        {
            throw new BusinessRuleException("Sadece kapali kayitlar yeniden acilabilir.");
        }

        item.RemainingAmount = item.OriginalAmount;
        item.Status = item.DueDate.Date < DateTime.UtcNow.Date
            ? ReceivablePayableStatus.Overdue
            : ReceivablePayableStatus.Open;

        await _context.SaveChangesAsync();
        return await MapReceivablePayableAsync(item, item.Status);
    }

    private async Task<Transaction> GetAccessibleTransactionAsync(Guid transactionId)
    {
        var transaction = await _context.Transactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == transactionId);

        var currentTenantId = _context.CurrentTenantId ?? Guid.Empty;
        var canAccess = transaction != null &&
                        !transaction.IsDeleted &&
                        (_context.CurrentUserIsSuperAdmin || transaction.TenantId == currentTenantId);

        if (!canAccess)
        {
            throw new NotFoundException("Islem kaydi bulunamadi.");
        }

        return transaction!;
    }

    private async Task<ReceivablePayable> GetAccessibleReceivablePayableAsync(Guid id)
    {
        var item = await _context.ReceivablePayables
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(current => current.Id == id);

        var currentTenantId = _context.CurrentTenantId ?? Guid.Empty;
        var canAccess = item != null &&
                        !item.IsDeleted &&
                        (_context.CurrentUserIsSuperAdmin || item.TenantId == currentTenantId);

        if (!canAccess)
        {
            throw new NotFoundException("Alacak / borc kaydi bulunamadi.");
        }

        return item!;
    }

    private async Task ValidateTransactionRequestAsync(
        TransactionType type,
        decimal amount,
        Guid? categoryId,
        string? relatedEntityType)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Transaction amount must be greater than zero");
        }

        if (type != TransactionType.Expense || !categoryId.HasValue)
        {
            return;
        }

        if (relatedEntityType == "GeneralExpense")
        {
            var category = await _context.GeneralExpenseCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item =>
                    item.Id == categoryId.Value &&
                    item.TenantId == SharedLookupTenantId &&
                    !item.IsDeleted);

            if (category == null)
            {
                throw new ValidationException("General expense category not found");
            }

            return;
        }

        var vehicleCategory = await _context.VehicleExpenseCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item =>
                item.Id == categoryId.Value &&
                item.TenantId == SharedLookupTenantId &&
                !item.IsDeleted);

        if (vehicleCategory == null)
        {
            throw new ValidationException("Vehicle expense category not found");
        }
    }

    private async Task<string?> ResolveCategoryNameAsync(Guid? categoryId, string? relatedEntityType)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        if (relatedEntityType == "GeneralExpense")
        {
            return await _context.GeneralExpenseCategories
                .IgnoreQueryFilters()
                .Where(category => category.Id == categoryId.Value && category.TenantId == SharedLookupTenantId && !category.IsDeleted)
                .Select(category => category.Name)
                .FirstOrDefaultAsync();
        }

        return await _context.VehicleExpenseCategories
            .IgnoreQueryFilters()
            .Where(category => category.Id == categoryId.Value && category.TenantId == SharedLookupTenantId && !category.IsDeleted)
            .Select(category => category.Name)
            .FirstOrDefaultAsync();
    }

    private static ReceivablePayableDto MapReceivablePayable(ReceivablePayable item, ReceivablePayableStatus status)
    {
        return new ReceivablePayableDto(
            item.Id,
            item.Type,
            item.SourceType,
            item.SourceId,
            null,
            item.CounterpartyName,
            item.PaymentMethod,
            item.DocumentType,
            item.DocumentNumber,
            item.IssueDate,
            item.DueDate,
            item.OriginalAmount,
            item.RemainingAmount,
            item.LastSettlementDate,
            status,
            item.Description);
    }

    private static ReceivablePayableDto MapReceivablePayable(ReceivablePayable item, ReceivablePayableStatus status, string? plate)
    {
        return new ReceivablePayableDto(
            item.Id,
            item.Type,
            item.SourceType,
            item.SourceId,
            plate,
            item.CounterpartyName,
            item.PaymentMethod,
            item.DocumentType,
            item.DocumentNumber,
            item.IssueDate,
            item.DueDate,
            item.OriginalAmount,
            item.RemainingAmount,
            item.LastSettlementDate,
            status,
            item.Description);
    }

    private async Task<ReceivablePayableDto> MapReceivablePayableAsync(ReceivablePayable item, ReceivablePayableStatus status)
    {
        string? plate = null;
        if (item.SourceType is ReceivablePayableSourceType.VehiclePurchase or ReceivablePayableSourceType.VehicleSale)
        {
            plate = await _context.Vehicles
                .IgnoreQueryFilters()
                .Where(vehicle => vehicle.Id == item.SourceId)
                .Select(vehicle => vehicle.Plate)
                .FirstOrDefaultAsync();
        }

        return MapReceivablePayable(item, status, plate);
    }
}
