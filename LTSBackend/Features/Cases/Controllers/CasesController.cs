using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Cases.Commands.CreateCase;
using LTSBackend.Features.Cases.Commands.DeleteCase;
using LTSBackend.Features.Cases.Commands.UpdateCase;
using LTSBackend.Features.Cases.Commands.UpdateCaseStatus;
using LTSBackend.Features.Cases.DTOs;
using LTSBackend.Features.Cases.Queries.GetAllCases;
using LTSBackend.Features.Cases.Queries.GetCaseById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LTSBackend.Features.Cases.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CasesController(IMediator _mediator, ILogger<CasesController> _logger) : ControllerBase
{
    // =====================================================
    // GET ALL CASES
    // =====================================================
    /// <summary>
    /// Role-based visibility (enforced in GetAllCasesHandler, not just here):
    /// - SuperAdmin: all cases, every firm
    /// - FirmAdmin: every case within their own firm
    /// - Partner: every case within their own firm ("View Firm Case Directory")
    /// - AssociateLawyer / Moharrir / InternParalegal: only cases they are
    ///   actively assigned to (CaseAssignments), scoped inside the handler
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.AllFirmUsersAndSuperAdmin)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchText,
        [FromQuery] int? courtID,
        [FromQuery] int? statusID,
        [FromQuery] string? priority,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Get all cases request - Page: {PageNumber}", pageNumber);

        var query = new GetAllCasesQuery(searchText, courtID, statusID, priority, pageNumber, pageSize);
        var result = await _mediator.Send(query);

        return Ok(ApiResponse<PagedResult<CaseDTO>>.SuccessResponse(
            result,
            "Cases successfully fetched"));
    }

    // =====================================================
    // GET CASE BY ID
    // =====================================================
    /// <summary>
    /// Role-based access (enforced in GetCaseByIdHandler, not just here):
    /// - SuperAdmin: any case, any firm
    /// - FirmAdmin / Partner: any case within their own firm
    /// - AssociateLawyer / Moharrir / InternParalegal: only if actively
    ///   assigned to this specific case — otherwise 404 (not 403, so the
    ///   case's existence isn't disclosed to a user who shouldn't see it)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = RoleNames.AllFirmUsersAndSuperAdmin)]
    public async Task<IActionResult> GetById(long id)
    {
        _logger.LogInformation("Get case by ID: {CaseID}", id);

        var query = new GetCaseByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<CaseDTO>.FailureResponse("Case not found"));
        }

        return Ok(ApiResponse<CaseDTO>.SuccessResponse(result, "Case successfully fetched"));
    }

    // =====================================================
    // CREATE NEW CASE
    // =====================================================
    /// <summary>
    /// Role-based: SuperAdmin, FirmAdmin, Partner only
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> Create([FromBody] CreateCaseDTO dto)
    {
        _logger.LogInformation("Create case: {CaseNumber}", dto.CaseNumber);

        var command = new CreateCaseCommand(
            dto.CaseNumber,
            dto.CaseTitle,
            dto.CaseDescription,
            dto.CourtID,
            dto.CategoryID,
            dto.Priority,
            dto.SubjectMatter,
            dto.FilingDate,
            dto.InstitutionDate,
            dto.RegistrationDate,
            dto.ExpectedDisposalDate,
            dto.ClaimedAmount,
            dto.PotentialLiability,
            dto.FinancialImplication,
            dto.ResponsibleDepartmentID,
            dto.CurrentLegalOfficerID);

        var caseID = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id = caseID },
            ApiResponse<long>.SuccessResponse(caseID, "Case successfully created"));
    }

    // =====================================================
    // UPDATE CASE
    // =====================================================
    /// <summary>
    /// Role-based: SuperAdmin, FirmAdmin, Partner only
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCaseDTO dto)
    {
        _logger.LogInformation("Update case: {CaseID}", id);

        if (id != dto.CaseID)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(
                "URL and body case ID do not match"));
        }

        var command = new UpdateCaseCommand(
            dto.CaseID,
            dto.CaseNumber,
            dto.CaseTitle,
            dto.CaseDescription,
            dto.CourtID,
            dto.CategoryID,
            dto.StageID,
            dto.Priority,
            dto.SubjectMatter,
            dto.ExpectedDisposalDate,
            dto.ClaimedAmount,
            dto.PotentialLiability,
            dto.CurrentLegalOfficerID,
            dto.IsArchived);

        var result = await _mediator.Send(command);

        return Ok(ApiResponse<bool>.SuccessResponse(result, "Case successfully updated"));
    }

    // =====================================================
    // DELETE CASE
    // =====================================================
    /// <summary>
    /// Role-based: SuperAdmin, FirmAdmin only
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(long id)
    {
        _logger.LogInformation("Delete case: {CaseID}", id);

        var command = new DeleteCaseCommand(id);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<bool>.SuccessResponse(result, "Case successfully deleted"));
    }

    // =====================================================
    // UPDATE CASE STATUS
    // =====================================================
    /// <summary>
    /// Role-based: SuperAdmin, FirmAdmin, Partner only
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateCaseStatusRequest request)
    {
        _logger.LogInformation("Update case status: {CaseID}", id);

        var command = new UpdateCaseStatusCommand(id, request.NewStatusID, request.Remarks);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<bool>.SuccessResponse(result, "Case status successfully updated"));
    }
}
