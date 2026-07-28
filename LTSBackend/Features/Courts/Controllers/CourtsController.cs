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
    // SECURITY NOTE (found during review - restricted, not silently left
    // as FirmAdminAndAbove): Court has NO FirmID column - see Models/
    // Masters/Court.cs and AppDbContext (no HasQueryFilter registered for
    // it either). It is genuinely GLOBAL, shared-across-every-tenant
    // reference data (the same real-world court is used by every firm's
    // cases). Previously any FirmAdmin - from ANY firm - could rename,
    // retype, or (if currently unreferenced) delete a court record that
    // OTHER firms' cases and hearings depend on, with no way for the
    // system to know or prevent it, since there is no tenant boundary on
    // this table to check against. That is a real cross-tenant data-
    // integrity risk today, not a hypothetical one.
    //
    // Restricted to SuperAdmin as an immediate, migration-free mitigation.
    // The SRS lists "Manage Courts" under Firm Admin's responsibilities,
    // so this is NOT the final fix - it trades away that feature to close
    // the live risk. The correct long-term fix is to make Court genuinely
    // per-tenant (add a FirmID column + EF migration + data backfill for
    // existing rows) so each firm can manage its own court list without
    // affecting others, then relax this back to FirmAdminAndAbove.
    // ================================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Create(CreateCourtCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Court created successfully."));
    }

    // =====================================================
    // UPDATE COURT
    // See Create() above for why this is SuperAdmin-only, not FirmAdminAndAbove.
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Update(int id, UpdateCourtCommand command)
    {
        if (id != command.CourtID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body CourtID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Court updated successfully."));
    }

    // =====================================================
    // DELETE COURT
    // See Create() above for why this is SuperAdmin-only, not FirmAdminAndAbove.
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.SuperAdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteCourtCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Court deleted successfully."));
    }
}
