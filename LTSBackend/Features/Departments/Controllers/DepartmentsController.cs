using LTSBackend.Comman.Responses;
using LTSBackend.Features.Departments.Commands.CreateDepartment;
using LTSBackend.Features.Departments.Commands.DeleteDepartment;
using LTSBackend.Features.Departments.Commands.UpdateDepartment;
using LTSBackend.Features.Departments.DTOs;
using LTSBackend.Features.Departments.Queries.GetAllDepartments;
using LTSBackend.Features.Departments.Queries.GetDepartmentById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Departments.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    // =====================================================
    // GET ALL DEPARTMENTS
    // Any authenticated user can read master data - required
    // for populating dropdowns on Case / User forms etc.
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var departments = await mediator.Send(new GetAllDepartmentsQuery(activeOnly));
        return Ok(ApiResponse<List<DepartmentDTO>>.SuccessResponse(departments));
    }

    // =====================================================
    // GET DEPARTMENT BY ID
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await mediator.Send(new GetDepartmentByIdQuery(id));
        return Ok(ApiResponse<DepartmentDTO>.SuccessResponse(department));
    }

    // =====================================================
    // CREATE DEPARTMENT
    // ================================================================
    // SECURITY NOTE: identical situation to CourtsController.Create -
    // Department has NO FirmID column (see Models/Masters/Department.cs),
    // so it is global/shared across every tenant, yet was open to
    // FirmAdminAndAbove. Any firm's FirmAdmin could previously rename or
    // delete a department that other firms' users/cases reference. The
    // SRS lists "Manage Departments" under Firm Admin, so this SuperAdmin
    // -only restriction is a temporary mitigation, not the final design -
    // the correct fix is adding a FirmID column (+ migration + backfill)
    // so departments are genuinely per-tenant, then relaxing this back.
    // ================================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Create(CreateDepartmentCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Department created successfully."));
    }

    // =====================================================
    // UPDATE DEPARTMENT
    // See Create() above for why this is SuperAdmin-only, not FirmAdminAndAbove.
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Update(int id, UpdateDepartmentCommand command)
    {
        if (id != command.DepartmentID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body DepartmentID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Department updated successfully."));
    }

    // =====================================================
    // DELETE DEPARTMENT
    // See Create() above for why this is SuperAdmin-only, not FirmAdminAndAbove.
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteDepartmentCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Department deleted successfully."));
    }
}
