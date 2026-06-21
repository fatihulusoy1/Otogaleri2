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
    private readonly IVehiclePhotoService _vehiclePhotoService;

    public VehiclesController(IVehicleService vehicleService, IVehiclePhotoService vehiclePhotoService)
    {
        _vehicleService = vehicleService;
        _vehiclePhotoService = vehiclePhotoService;
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<VehicleLookupsDto>> GetLookups()
    {
        return Ok(await _vehicleService.GetLookupsAsync());
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

    [HttpPut("purchases/{id}")]
    public async Task<ActionResult<PurchaseRecordDto>> UpdatePurchase(Guid id, UpdatePurchaseRequest request)
    {
        return Ok(await _vehicleService.UpdatePurchaseAsync(id, request));
    }

    [HttpDelete("purchases/{id}")]
    public async Task<IActionResult> DeletePurchase(Guid id)
    {
        await _vehicleService.DeletePurchaseAsync(id);
        return NoContent();
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

    [HttpPut("{id}/sales")]
    public async Task<ActionResult<VehicleSaleDto>> UpdateSale(Guid id, UpdateVehicleSaleRequest request)
    {
        return Ok(await _vehicleService.UpdateSaleAsync(id, request));
    }

    [HttpDelete("{id}/sales")]
    public async Task<IActionResult> DeleteSale(Guid id)
    {
        await _vehicleService.DeleteSaleAsync(id);
        return NoContent();
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

    [HttpPut("expenses/{id}")]
    public async Task<ActionResult<VehicleExpenseDto>> UpdateExpense(Guid id, UpdateVehicleExpenseRequest request)
    {
        return Ok(await _vehicleService.UpdateExpenseAsync(id, request));
    }

    [HttpDelete("expenses/{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        await _vehicleService.DeleteExpenseAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/photos")]
    public async Task<ActionResult<List<VehiclePhotoDto>>> GetPhotos(Guid id)
    {
        return Ok(await _vehiclePhotoService.GetByVehicleAsync(id));
    }

    [HttpPost("{id}/photos")]
    [RequestSizeLimit(52_428_800)] // 50 MB: birden fazla fotograf icin
    public async Task<ActionResult<List<VehiclePhotoDto>>> UploadPhotos(Guid id, [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "Yuklenecek fotograf bulunamadi." });
        }

        var inputs = files
            .Select(file => new PhotoUploadInput(file.OpenReadStream(), file.FileName, file.ContentType))
            .ToList();

        var photos = await _vehiclePhotoService.UploadAsync(id, inputs);
        return Ok(photos);
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId)
    {
        await _vehiclePhotoService.DeleteAsync(photoId);
        return NoContent();
    }

    [HttpPut("photos/{photoId}/cover")]
    public async Task<ActionResult<List<VehiclePhotoDto>>> SetPhotoCover(Guid photoId)
    {
        return Ok(await _vehiclePhotoService.SetCoverAsync(photoId));
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
