using AutoGallerySaaS.Application.Features.Vehicles.Dtos;
using AutoGallerySaaS.Application.Features.Vehicles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetAll()
    {
        return Ok(await _vehicleService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return Ok(vehicle);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleRequest request)
    {
        var vehicle = await _vehicleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<List<PurchaseRecordDto>>> GetPurchases()
    {
        return Ok(await _vehicleService.GetPurchasesAsync());
    }

    [HttpPost("purchases")]
    public async Task<ActionResult<PurchaseRecordDto>> CreatePurchase(CreatePurchaseRequest request)
    {
        var purchase = await _vehicleService.CreatePurchaseAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = purchase.VehicleId }, purchase);
    }

    [HttpGet("sales")]
    public async Task<ActionResult<List<VehicleSaleDto>>> GetSales()
    {
        return Ok(await _vehicleService.GetSalesAsync());
    }

    [HttpPost("{id}/sales")]
    public async Task<ActionResult<VehicleSaleDto>> CompleteSale(Guid id, CompleteVehicleSaleRequest request)
    {
        return Ok(await _vehicleService.CompleteSaleAsync(id, request));
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<List<VehicleExpenseDto>>> GetExpenses([FromQuery] Guid? vehicleId)
    {
        return Ok(await _vehicleService.GetExpensesAsync(vehicleId));
    }

    [HttpPost("{id}/expenses")]
    public async Task<ActionResult<VehicleExpenseDto>> AddExpense(Guid id, CreateVehicleExpenseRequest request)
    {
        return Ok(await _vehicleService.AddExpenseAsync(id, request));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleRequest request)
    {
        await _vehicleService.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _vehicleService.DeleteAsync(id);
        return NoContent();
    }
}
