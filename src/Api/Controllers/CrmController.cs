using AutoGallerySaaS.Application.Features.Crm.Dtos;
using AutoGallerySaaS.Application.Features.Crm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CrmController : ControllerBase
{
    private readonly ICrmService _crmService;

    public CrmController(ICrmService crmService)
    {
        _crmService = crmService;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerDto>>> GetCustomers()
    {
        return Ok(await _crmService.GetCustomersAsync());
    }

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerRequest request)
    {
        return Ok(await _crmService.CreateCustomerAsync(request));
    }
}
