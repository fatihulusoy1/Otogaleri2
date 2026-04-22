using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Services;
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
}
