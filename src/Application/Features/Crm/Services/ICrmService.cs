using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Crm.Dtos;

namespace AutoGallerySaaS.Application.Features.Crm.Services;

public interface ICrmService
{
    Task<List<CustomerDto>> GetCustomersAsync();
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
}
