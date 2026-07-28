using LTSBackend.Comman.Responses;
using LTSBackend.Features.CaseCategories.Commands.CreateCaseCategory;
using LTSBackend.Features.CaseCategories.Commands.DeleteCaseCategory;
using LTSBackend.Features.CaseCategories.Commands.UpdateCaseCategory;
using LTSBackend.Features.CaseCategories.DTOs;
using LTSBackend.Features.CaseCategories.Queries.GetAllCaseCategories;
using LTSBackend.Features.CaseCategories.Queries.GetCaseCategoryById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.CaseCategories.Controllers;

/// <summary>
/// Master data for case categories (e.g. Civil, Criminal, Corporate).
/// Same per-tenant model as Courts/Departments - FirmID is nullable on
/// CaseCategory: NULL is a system-wide global category managed by
/// SuperAdmin and visible to every firm, a real value is a firm's own
/// custom category visible/editable only by that firm. See
/// CreateCaseCategoryHandler/UpdateCaseCategoryHandler/DeleteCaseCategoryHandler
/// for the ownership enforcement.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CaseCategoriesController(IMediator mediator) : ControllerBase
{
    // Any authenticated user can read master data (needed for Case forms/dropdowns).
    // Results are automatically scoped (global + own firm) by the HasQueryFilter on CaseCategory.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchText, [FromQuery] bool activeOnly = true)
    {
        var categories = await mediator.Send(new GetAllCaseCategoriesQuery(searchText, activeOnly));
        return Ok(ApiResponse<List<CaseCategoryDTO>>.SuccessResponse(categories));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await mediator.Send(new GetCaseCategoryByIdQuery(id));
        return Ok(ApiResponse<CaseCategoryDTO>.SuccessResponse(category));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create(CreateCaseCategoryCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Case category created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, UpdateCaseCategoryCommand command)
    {
        if (id != command.CategoryID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body CategoryID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Case category updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteCaseCategoryCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Case category deleted successfully."));
    }
}
