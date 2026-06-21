using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Services;
using AutoGallerySaaS.Domain.Entities.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("expense-categories")]
    public async Task<ActionResult<List<ExpenseCategoryDto>>> GetExpenseCategories([FromQuery] ExpenseCategoryType? categoryType)
    {
        return Ok(await _financeService.GetExpenseCategoriesAsync(categoryType));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions()
    {
        return Ok(await _financeService.GetTransactionsAsync());
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionRequest request)
    {
        return Ok(await _financeService.CreateTransactionAsync(request));
    }

    [HttpPut("transactions/{id:guid}")]
    public async Task<ActionResult<TransactionDto>> UpdateTransaction(Guid id, UpdateTransactionRequest request)
    {
        return Ok(await _financeService.UpdateTransactionAsync(id, request));
    }

    [HttpDelete("transactions/{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        await _financeService.DeleteTransactionAsync(id);
        return NoContent();
    }

    [HttpGet("receivables-payables")]
    public async Task<ActionResult<List<ReceivablePayableDto>>> GetReceivablePayables([FromQuery] ReceivablePayableType type)
    {
        return Ok(await _financeService.GetReceivablePayablesAsync(type));
    }

    [HttpPost("receivables-payables/{id:guid}/settle")]
    public async Task<ActionResult<ReceivablePayableDto>> SettleReceivablePayable(Guid id, SettleReceivablePayableRequest request)
    {
        return Ok(await _financeService.SettleReceivablePayableAsync(id, request.SettlementDate));
    }

    [HttpPost("receivables-payables/{id:guid}/partial-settlement")]
    public async Task<ActionResult<ReceivablePayableDto>> ApplyPartialSettlement(Guid id, PartialSettlementRequest request)
    {
        return Ok(await _financeService.ApplyPartialSettlementAsync(id, request.Amount, request.SettlementDate));
    }

    [HttpPost("receivables-payables/{id:guid}/mark-overdue")]
    public async Task<ActionResult<ReceivablePayableDto>> MarkReceivablePayableOverdue(Guid id)
    {
        return Ok(await _financeService.MarkReceivablePayableOverdueAsync(id));
    }

    [HttpPost("receivables-payables/{id:guid}/reopen")]
    public async Task<ActionResult<ReceivablePayableDto>> ReopenReceivablePayable(Guid id)
    {
        return Ok(await _financeService.ReopenReceivablePayableAsync(id));
    }
}
