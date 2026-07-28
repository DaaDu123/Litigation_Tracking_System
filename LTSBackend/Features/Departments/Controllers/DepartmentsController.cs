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
    // Query results are automatically scoped by the caller's visibility
    // (system-wide global departments + their own firm's custom ones) via
    // the HasQueryFilter on Department in AppDbContext.
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
    // ARCHITECTURE FIX APPLIED: same per-tenant model as Court - FirmID is
    // nullable (NULL = system-wide global department; a real value = a
    // firm's own custom department). CreateDepartmentHandler assigns
    // ownership on create, Update/DeleteDepartmentHandler enforce that a
    // FirmAdmin may only touch their OWN firm's custom departments. This
    // replaced an earlier temporary SuperAdmin-only lockdown. Requires the
    // pending EF migration that adds Department.FirmID before deployment.
    // ================================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create(CreateDepartmentCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Department created successfully."));
    }

    // =====================================================
    // UPDATE DEPARTMENT
    // Firm Admin may only update their OWN firm's custom department
    // (enforced in UpdateDepartmentHandler).
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, UpdateDepartmentCommand command)
    {
        if (id != command.DepartmentID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body DepartmentID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Department updated successfully."));
    }

    // =====================================================
    // DELETE DEPARTMENT
    // Firm Admin may only delete their OWN firm's custom department
    // (enforced in DeleteDepartmentHandler).
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteDepartmentCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Department deleted successfully."));
    }
}
