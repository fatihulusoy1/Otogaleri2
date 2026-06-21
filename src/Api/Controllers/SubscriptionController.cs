using AutoGallerySaaS.Application.Features.Subscription.Dtos;
using AutoGallerySaaS.Application.Features.Subscription.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
    {
        return Ok(await _subscriptionService.GetPlansAsync());
    }

    [HttpGet]
    public async Task<ActionResult<SubscriptionStatusDto>> GetCurrent()
    {
        return Ok(await _subscriptionService.GetCurrentAsync());
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<SubscriptionStatusDto>> Checkout(CheckoutRequest request)
    {
        return Ok(await _subscriptionService.CheckoutAsync(request));
    }
}
