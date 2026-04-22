using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Vehicles.Dtos;
using AutoGallerySaaS.Domain.Entities.Vehicles;

using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public class VehicleService : IVehicleService
{
    private readonly IApplicationDbContext _context;

    public VehicleService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleDto>> GetAllAsync()
    {
        return await _context.Vehicles
            .Select(v => new VehicleDto(v.Id, v.Plate, v.Brand, v.Model, v.Year, v.Color, v.PurchasePrice, v.TargetSalePrice, v.Status, v.Description))
            .ToListAsync();
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var v = await _context.Vehicles.FindAsync(id);
        if (v == null) return null;
        return new VehicleDto(v.Id, v.Plate, v.Brand, v.Model, v.Year, v.Color, v.PurchasePrice, v.TargetSalePrice, v.Status, v.Description);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        var vehicle = new Vehicle
        {
            Plate = request.Plate,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Color = request.Color,
            EngineNumber = request.EngineNumber,
            ChassisNumber = request.ChassisNumber,
            PurchasePrice = request.PurchasePrice,
            TargetSalePrice = request.TargetSalePrice,
            Status = VehicleStatus.InStock
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return new VehicleDto(vehicle.Id, vehicle.Plate, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.Color, vehicle.PurchasePrice, vehicle.TargetSalePrice, vehicle.Status, vehicle.Description);
    }

    public async Task UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null) throw new Exception("Vehicle not found");

        vehicle.Plate = request.Plate;
        vehicle.Brand = request.Brand;
        vehicle.Model = request.Model;
        vehicle.Year = request.Year;
        vehicle.Color = request.Color;
        vehicle.PurchasePrice = request.PurchasePrice;
        vehicle.TargetSalePrice = request.TargetSalePrice;
        vehicle.Status = request.Status;
        vehicle.Description = request.Description;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle != null)
        {
            vehicle.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
