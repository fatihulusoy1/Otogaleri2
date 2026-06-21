using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Application.Features.TenantActivities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize(Roles = "TenantAdmin")]
[ApiController]
[Route("api/tenant/activities")]
public class TenantActivityController : ControllerBase
{
    private readonly ITenantActivityService _activityService;

    public TenantActivityController(ITenantActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantActivityDto>>> GetActivities()
    {
        return Ok(await _activityService.GetActivitiesAsync());
    }
}
