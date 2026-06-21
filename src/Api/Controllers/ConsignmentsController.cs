using AutoGallerySaaS.Application.Features.Consignments.Dtos;
using AutoGallerySaaS.Application.Features.Consignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConsignmentsController : ControllerBase
{
    private readonly IConsignmentService _consignmentService;

    public ConsignmentsController(IConsignmentService consignmentService)
    {
        _consignmentService = consignmentService;
    }

    [HttpGet("brokered")]
    public async Task<ActionResult<List<BrokeredConsignmentDto>>> GetBrokered()
    {
        return Ok(await _consignmentService.GetBrokeredAsync());
    }

    [HttpPost("brokered")]
    public async Task<ActionResult<BrokeredConsignmentDto>> CreateBrokered(CreateBrokeredConsignmentRequest request)
    {
        return Ok(await _consignmentService.CreateBrokeredAsync(request));
    }

    [HttpPut("brokered/{id:guid}")]
    public async Task<ActionResult<BrokeredConsignmentDto>> UpdateBrokered(Guid id, UpdateBrokeredConsignmentRequest request)
    {
        return Ok(await _consignmentService.UpdateBrokeredAsync(id, request));
    }

    [HttpDelete("brokered/{id:guid}")]
    public async Task<IActionResult> DeleteBrokered(Guid id)
    {
        await _consignmentService.DeleteBrokeredAsync(id);
        return NoContent();
    }

    [HttpGet("stock")]
    public async Task<ActionResult<List<StockConsignmentDto>>> GetStock()
    {
        return Ok(await _consignmentService.GetStockAsync());
    }

    [HttpPost("stock")]
    public async Task<ActionResult<StockConsignmentDto>> CreateStock(CreateStockConsignmentRequest request)
    {
        return Ok(await _consignmentService.CreateStockAsync(request));
    }

    [HttpPut("stock/{id:guid}")]
    public async Task<ActionResult<StockConsignmentDto>> UpdateStock(Guid id, UpdateStockConsignmentRequest request)
    {
        return Ok(await _consignmentService.UpdateStockAsync(id, request));
    }

    [HttpDelete("stock/{id:guid}")]
    public async Task<IActionResult> DeleteStock(Guid id)
    {
        await _consignmentService.DeleteStockAsync(id);
        return NoContent();
    }

    [HttpPost("stock/{id:guid}/sale")]
    public async Task<ActionResult<StockConsignmentDto>> CompleteStockSale(Guid id, CompleteStockConsignmentSaleRequest request)
    {
        return Ok(await _consignmentService.CompleteStockSaleAsync(id, request));
    }

    [HttpPut("stock/{id:guid}/sale")]
    public async Task<ActionResult<StockConsignmentDto>> UpdateStockSale(Guid id, CompleteStockConsignmentSaleRequest request)
    {
        return Ok(await _consignmentService.UpdateStockSaleAsync(id, request));
    }

    [HttpDelete("stock/{id:guid}/sale")]
    public async Task<IActionResult> DeleteStockSale(Guid id)
    {
        await _consignmentService.DeleteStockSaleAsync(id);
        return NoContent();
    }
}
