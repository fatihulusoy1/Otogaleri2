using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Services;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.UnitTests.TestSupport;
using FluentAssertions;

namespace AutoGallerySaaS.UnitTests.Finance;

public class FinanceServiceTests
{
    private static ReceivablePayable NewReceivable(Guid tenantId, decimal amount = 1000m, ReceivablePayableStatus status = ReceivablePayableStatus.Open)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = ReceivablePayableType.Receivable,
            SourceType = ReceivablePayableSourceType.VehicleSale,
            SourceId = Guid.NewGuid(),
            CounterpartyName = "Test Musteri",
            PaymentMethod = PaymentMethod.Deferred,
            DocumentType = FinancialDocumentType.OpenAccount,
            IssueDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            OriginalAmount = amount,
            RemainingAmount = amount,
            Status = status
        };

    [Fact]
    public async Task ApplyPartialSettlementAsync_NonPositiveAmount_ThrowsValidationException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var act = () => sut.ApplyPartialSettlementAsync(receivable.Id, 0m, DateTime.UtcNow);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ApplyPartialSettlementAsync_PartialAmount_ReducesRemainingAndMarksPartiallyPaid()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId, amount: 1000m);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var result = await sut.ApplyPartialSettlementAsync(receivable.Id, 400m, DateTime.UtcNow);

        result.RemainingAmount.Should().Be(600m);
        result.Status.Should().Be(ReceivablePayableStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ApplyPartialSettlementAsync_FullAmount_ClosesRecord()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId, amount: 1000m);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var result = await sut.ApplyPartialSettlementAsync(receivable.Id, 1000m, DateTime.UtcNow);

        result.RemainingAmount.Should().Be(0m);
        result.Status.Should().Be(ReceivablePayableStatus.Closed);
    }

    [Fact]
    public async Task ApplyPartialSettlementAsync_AmountAboveRemaining_ThrowsValidationException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId, amount: 1000m);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var act = () => sut.ApplyPartialSettlementAsync(receivable.Id, 1500m, DateTime.UtcNow);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SettleReceivablePayableAsync_ZeroesRemainingAndCloses()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId, amount: 1000m);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var result = await sut.SettleReceivablePayableAsync(receivable.Id, DateTime.UtcNow);

        result.RemainingAmount.Should().Be(0m);
        result.Status.Should().Be(ReceivablePayableStatus.Closed);
        result.LastSettlementDate.Should().NotBeNull();
    }

    [Fact]
    public async Task ReopenReceivablePayableAsync_OnOpenRecord_ThrowsBusinessRuleException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var receivable = NewReceivable(tenantId, status: ReceivablePayableStatus.Open);
        context.ReceivablePayables.Add(receivable);
        await context.SaveChangesAsync();

        var sut = new FinanceService(context);

        var act = () => sut.ReopenReceivablePayableAsync(receivable.Id);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task GetAccessibleReceivablePayable_ForUnknownId_ThrowsNotFoundException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var sut = new FinanceService(context);

        var act = () => sut.SettleReceivablePayableAsync(Guid.NewGuid(), DateTime.UtcNow);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTransactionAsync_NonPositiveAmount_ThrowsValidationException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var sut = new FinanceService(context);

        var request = new CreateTransactionRequest(
            TransactionType.Income,
            0m,
            DateTime.UtcNow,
            "Test",
            PaymentMethod.Cash,
            null,
            null,
            null);

        var act = () => sut.CreateTransactionAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
