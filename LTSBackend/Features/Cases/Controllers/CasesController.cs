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
    /// Role-based: 
    /// - SuperAdmin: tamam cases
    /// - FirmAdmin: apne firm ke cases
    /// - Partner: assigned cases
    /// - AssociateLawyer: assigned cases only
    /// - Moharrir: assigned cases
    /// - InternParalegal: nahi access (403 Forbidden)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
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
    /// Role-based access:
    /// - SuperAdmin: tamam cases
    /// - FirmAdmin: apne firm ke cases
    /// - Partner: assigned cases
    /// - AssociateLawyer: assigned cases
    /// - Moharrir: assigned cases
    /// - InternParalegal: read-only access if assigned
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetById(long id)
    {
        _logger.LogInformation("Get case by ID: {CaseID}", id);

        var query = new GetCaseByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<CaseDTO>.FailureResponse("Case nahi mila"));
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
                "URL aur body mein case ID match nahi hain"));
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
