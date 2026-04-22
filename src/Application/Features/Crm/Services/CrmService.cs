using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Crm.Dtos;
using AutoGallerySaaS.Domain.Entities.Crm;

using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Crm.Services;

public class CrmService : ICrmService
{
    private readonly IApplicationDbContext _context;

    public CrmService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        return await _context.Customers
            .Select(c => new CustomerDto(c.Id, c.FirstName, c.LastName, c.Email, c.Phone))
            .ToListAsync();
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerDto(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.Phone);
    }
}
