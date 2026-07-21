using LTSBackend.Comman.Responses;
using LTSBackend.Features.Deadlines.Commands.CompleteDeadline;
using LTSBackend.Features.Deadlines.Commands.CreateDeadline;
using LTSBackend.Features.Deadlines.Commands.DeleteDeadline;
using LTSBackend.Features.Deadlines.Commands.UpdateDeadline;
using LTSBackend.Features.Deadlines.DTOs;
using LTSBackend.Features.Deadlines.Queries.GetCaseDeadlines;
using LTSBackend.Features.Deadlines.Queries.GetUpcomingDeadlines;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Deadlines.Controllers;

/// <summary>
/// SRS Reference: Complete Database Schema - Deadlines table
/// Litigation_Tracking_System_Case_SRS.docx Section 5.3 "Hearing and Deadline Monitoring"
/// FR-07, FR-08 - Track legal deadlines and generate reminders/alerts
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DeadlinesController(IMediator _mediator) : ControllerBase
{
    // GET api/deadlines/upcoming
    [HttpGet("upcoming")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetUpcoming([FromQuery] int? daysAhead)
    {
        var result = await _mediator.Send(new GetUpcomingDeadlinesQuery { DaysAhead = daysAhead });
        return Ok(ApiResponse<List<DeadlineDetailDTO>>.SuccessResponse(result, "Upcoming deadlines fetched"));
    }

    // GET api/deadlines/case/5?completed=false
    [HttpGet("case/{caseId}")]
    [Authorize(Roles = RoleNames.AllFirmUsers)]
    public async Task<IActionResult> GetByCase(long caseId, [FromQuery] bool? completed)
    {
        var result = await _mediator.Send(new GetCaseDeadlinesQuery { CaseID = caseId, Completed = completed });
        return Ok(ApiResponse<List<DeadlineDetailDTO>>.SuccessResponse(result, "Case deadlines fetched"));
    }

    // POST api/deadlines
    [HttpPost]
    [Authorize(Roles = RoleNames.AllLawyers)]
    public async Task<IActionResult> Create([FromBody] CreateDeadlineDTO dto)
    {
        var id = await _mediator.Send(new CreateDeadlineCommand { Deadline = dto });
        return CreatedAtAction(nameof(GetByCase), new { caseId = dto.CaseID }, ApiResponse<long>.SuccessResponse(id, "Deadline successfully created"));
    }

    // PUT api/deadlines/12
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.AllLawyers)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDeadlineDTO dto)
    {
        dto.DeadlineID = id;
        var result = await _mediator.Send(new UpdateDeadlineCommand { Deadline = dto });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Deadline successfully updated"));
    }

    // PUT api/deadlines/12/complete
    [HttpPut("{id}/complete")]
    [Authorize(Roles = RoleNames.AllLawyers)]
    public async Task<IActionResult> Complete(long id)
    {
        var result = await _mediator.Send(new CompleteDeadlineCommand { DeadlineID = id });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Deadline marked complete"));
    }

    // DELETE api/deadlines/12
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _mediator.Send(new DeleteDeadlineCommand { DeadlineID = id });
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Deadline successfully deleted"));
    }
}
