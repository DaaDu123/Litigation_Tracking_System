using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Permissions.Commands.AssignPermissions;
using LTSBackend.Features.Permissions.DTOs;
using LTSBackend.Features.Permissions.Queries.GetAllPermissions;
using LTSBackend.Features.Permissions.Queries.GetRolePermissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Permissions.Controllers;

[Route("api/[controller]")]
[ApiController]
[HasPermission("ManageRoles")]
public class PermissionsController(IMediator mediator): ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await mediator.Send(new GetAllPermissionsQuery());
        return Ok(ApiResponse<List<PermissionDTO>>.SuccessResponse(permissions,"Permissions fetched successfully."));
    }

    [HttpGet("role/{roleId}")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var permissions = await mediator.Send(new GetRolePermissionsQuery(roleId));
        return Ok(ApiResponse<List<PermissionDTO>>.SuccessResponse(permissions,"Role permissions fetched successfully."));
    }

    [HttpPut("assign")]
    public async Task<IActionResult> AssignPermissions(AssignPermissionsCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result,"Permissions assigned successfully."));
    }
}