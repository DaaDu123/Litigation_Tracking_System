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
public class DashboardController(IMediator mediator): ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var result = await mediator.Send(new GetDashboardStatsQuery());

        return Ok(ApiResponse<DashboardDTO>.SuccessResponse(result,"Dashboard statistics fetched successfully."));
    }
}