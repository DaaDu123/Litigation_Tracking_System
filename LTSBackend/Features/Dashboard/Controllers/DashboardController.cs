using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Dashboard.DTOs;
using LTSBackend.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Dashboard.Controllers;

[Route("api/[controller]")]
[ApiController]
[HasPermission("ViewDashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // =====================================================
    // GET DASHBOARD STATISTICS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        _logger.LogInformation("Get dashboard stats request");

        var result = await _mediator.Send(new GetDashboardStatsQuery());

        return Ok(ApiResponse<DashboardDTO>.SuccessResponse(
            result,
            "Dashboard statistics fetched successfully."));
    }
}