using LTSBackend.Comman.Responses;
using LTSBackend.Features.CaseParties.Commands.CreateCaseParty;
using LTSBackend.Features.CaseParties.Commands.DeleteCaseParty;
using LTSBackend.Features.CaseParties.Commands.UpdateCaseParty;
using LTSBackend.Features.CaseParties.DTOs;
using LTSBackend.Features.CaseParties.Queries.GetCaseParties;
using LTSBackend.Features.CaseParties.Queries.GetCasePartyById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.CaseParties.Controllers;

/// <summary>
/// SRS Reference: Complete Database Schema - CaseParties table
/// Litigation_Tracking_System_Case_SRS.docx Section 5.4 "Party Information"
/// Plaintiff / Petitioner / Defendant / Respondent management per case
/// </summary>
[Route("api/cases/{caseId}/parties")]
[ApiController]
[Authorize]
public class CasePartiesController(IMediator _mediator) : ControllerBase
{
    // GET api/cases/5/parties
    [HttpGet]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetByCase(long caseId)
    {
        var result = await _mediator.Send(new GetCasePartiesQuery { CaseID = caseId });
        return Ok(ApiResponse<List<CasePartyDetailDTO>>.SuccessResponse(result, "Case parties fetched"));
    }

    // GET api/cases/5/parties/12
    [HttpGet("{partyId}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetById(long caseId, long partyId)
    {
        var result = await _mediator.Send(new GetCasePartyByIdQuery { PartyID = partyId });
        return Ok(ApiResponse<CasePartyDetailDTO>.SuccessResponse(result, "Party fetched"));
    }

    // POST api/cases/5/parties
    [HttpPost]
    [Authorize(Roles = RoleNames.AllLawyers)]
    public async Task<IActionResult> Create(long caseId, [FromBody] CreateCasePartyDTO dto)
    {
        dto.CaseID = caseId;
        var partyId = await _mediator.Send(new CreateCasePartyCommand { Party = dto });
        return CreatedAtAction(nameof(GetById), new { caseId, partyId }, ApiResponse<long>.SuccessResponse(partyId, "Party successfully added"));
    }

    // PUT api/cases/5/parties/12
    [HttpPut("{partyId}")]
    [Authorize(Roles = RoleNames.AllLawyers)]
    public async Task<IActionResult> Update(long caseId, long partyId, [FromBody] UpdateCasePartyDTO dto)
    {
        dto.PartyID = partyId;
        var result = await _mediator.Send(new UpdateCasePartyCommand { Party = dto });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Party successfully updated"));
    }

    // DELETE api/cases/5/parties/12
    [HttpDelete("{partyId}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> Delete(long caseId, long partyId)
    {
        var result = await _mediator.Send(new DeleteCasePartyCommand { PartyID = partyId });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Party successfully deleted"));
    }
}
