using LTSBackend.Comman.Responses;
using LTSBackend.Features.AuditLogs.DTOs;
using LTSBackend.Features.AuditLogs.Queries;
using LTSBackend.Features.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.AuditLogs.Controllers;

[Route("api/[controller]")]
[ApiController]
[HasPermission("ViewAuditLogs")]
public class AuditLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await mediator.Send(new GetAuditLogsQuery());
        return Ok(ApiResponse<List<AuditLogDTO>>.SuccessResponse(logs,"Audit logs fetched successfully."));
    }
}