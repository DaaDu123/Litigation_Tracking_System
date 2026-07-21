using LTSBackend.Comman.Responses;
using LTSBackend.Features.CaseNotes.Commands.CreateNote;
using LTSBackend.Features.CaseNotes.Commands.DeleteNote;
using LTSBackend.Features.CaseNotes.Commands.UpdateNote;
using LTSBackend.Features.CaseNotes.DTOs;
using LTSBackend.Features.CaseNotes.Queries.GetCaseNotes;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.CaseNotes.Controllers;

/// <summary>
/// SRS Reference: Complete Database Schema - CaseNotes table
/// Litigation_Tracking_System_Case_SRS.docx - Lawyer Functions: "Add case notes, Record legal opinions"
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CaseNotesController(IMediator _mediator) : ControllerBase
{
    // GET api/casenotes/case/5
    [HttpGet("case/{caseId}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetByCase(long caseId)
    {
        var result = await _mediator.Send(new GetCaseNotesQuery { CaseID = caseId });
        return Ok(ApiResponse<List<CaseNoteDetailDTO>>.SuccessResponse(result, "Case notes fetched"));
    }

    // POST api/casenotes
    [HttpPost]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> Create([FromBody] CreateCaseNoteDTO dto)
    {
        var id = await _mediator.Send(new CreateCaseNoteCommand { Note = dto });
        return CreatedAtAction(nameof(GetByCase), new { caseId = dto.CaseID }, ApiResponse<long>.SuccessResponse(id, "Note successfully added"));
    }

    // PUT api/casenotes/12
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCaseNoteDTO dto)
    {
        dto.NoteID = id;
        var result = await _mediator.Send(new UpdateCaseNoteCommand { Note = dto });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Note successfully updated"));
    }

    // DELETE api/casenotes/12
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _mediator.Send(new DeleteCaseNoteCommand { NoteID = id });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Note successfully deleted"));
    }
}
