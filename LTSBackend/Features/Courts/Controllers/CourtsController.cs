using LTSBackend.Comman.Responses;
using LTSBackend.Features.Courts.Commands.CreateCourt;
using LTSBackend.Features.Courts.Commands.DeleteCourt;
using LTSBackend.Features.Courts.Commands.UpdateCourt;
using LTSBackend.Features.Courts.DTOs;
using LTSBackend.Features.Courts.Queries.GetAllCourts;
using LTSBackend.Features.Courts.Queries.GetCourtById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Courts.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourtsController(IMediator mediator) : ControllerBase
{
    // =====================================================
    // GET ALL COURTS
    // Any authenticated user can read master data.
    // Default: activeOnly=true (dropdown use case)
    // Admin panel: pass activeOnly=false to see all records
    // Query results are automatically scoped by the caller's visibility
    // (system-wide global courts + their own firm's custom courts) via
    // the HasQueryFilter on Court in AppDbContext.
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchText,[FromQuery] bool activeOnly = true)
    {
        var courts = await mediator.Send(new GetAllCourtsQuery(searchText, activeOnly));
        return Ok(ApiResponse<List<CourtDTO>>.SuccessResponse(courts));
    }

    // =====================================================
    // GET COURT BY ID
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var court = await mediator.Send(new GetCourtByIdQuery(id));
        return Ok(ApiResponse<CourtDTO>.SuccessResponse(court));
    }

    // =====================================================
    // CREATE COURT
    // ================================================================
    // ARCHITECTURE FIX APPLIED: Court is now genuinely per-tenant-aware.
    // FirmID is nullable on the Court entity - NULL means a system-wide
    // global court (managed by SuperAdmin, visible to every firm), a
    // real value means a firm's own custom court entry. CreateCourtHandler
    // sets FirmID = null for SuperAdmin or the caller's own FirmID for
    // FirmAdmin, and UpdateCourtHandler/DeleteCourtHandler enforce that a
    // FirmAdmin may only touch their OWN firm's custom courts - never a
    // global entry or another firm's. This replaced an earlier temporary
    // SuperAdmin-only lockdown that existed before Court had any tenant
    // boundary at all. Requires the pending EF migration that adds the
    // Court.FirmID column to be applied before deployment.
    // ================================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create(CreateCourtCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Court created successfully."));
    }

    // =====================================================
    // UPDATE COURT
    // Firm Admin may only update their OWN firm's custom court (enforced
    // in UpdateCourtHandler) - never a global or another firm's court.
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, UpdateCourtCommand command)
    {
        if (id != command.CourtID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body CourtID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Court updated successfully."));
    }

    // =====================================================
    // DELETE COURT
    // Firm Admin may only delete their OWN firm's custom court (enforced
    // in DeleteCourtHandler) - never a global or another firm's court.
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteCourtCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Court deleted successfully."));
    }
}
