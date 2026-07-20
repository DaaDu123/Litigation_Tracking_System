using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LTSBackend.Comman.Responses;
using LTSBackend.Features.Hearings.Commands.CreateHearing;
using LTSBackend.Features.Hearings.Commands.DeleteHearing;
using LTSBackend.Features.Hearings.Commands.UpdateHearing;
using LTSBackend.Features.Hearings.DTOs;
using LTSBackend.Features.Hearings.Queries.GetCaseHearings;
using LTSBackend.Features.Hearings.Queries.GetHearingById;
using LTSBackend.Features.Hearings.Queries.GetUpcomingHearings;

namespace LTSBackend.Features.Hearings.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HearingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HearingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<long>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> CreateHearing([FromBody] CreateHearingDTO dto)
        {
            var command = new CreateHearingCommand { Hearing = dto };
            var hearingId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetHearingById), new { id = hearingId },
                new ApiResponse<long> { Success = true, Data = hearingId, Message = "Hearing created successfully" });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HearingDetailDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetHearingById(long id)
        {
            var query = new GetHearingByIdQuery { HearingId = id };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<HearingDetailDTO> { Success = true, Data = result });
        }

        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(ApiResponse<PagedHearingResult<HearingDetailDTO>>), 200)]
        public async Task<IActionResult> GetUpcomingHearings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] long? caseId = null,
            [FromQuery] int? courtId = null)
        {
            var query = new GetUpcomingHearingsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                CaseId = caseId,
                CourtId = courtId
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PagedHearingResult<HearingDetailDTO>> { Success = true, Data = result });
        }

        [HttpGet("case/{caseId}")]
        [ProducesResponseType(typeof(ApiResponse<PagedHearingResult<HearingDetailDTO>>), 200)]
        public async Task<IActionResult> GetCaseHearings(
            long caseId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetCaseHearingsQuery
            {
                CaseId = caseId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PagedHearingResult<HearingDetailDTO>> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> UpdateHearing(long id, [FromBody] UpdateHearingDTO dto)
        {
            dto.HearingId = id;
            var command = new UpdateHearingCommand { Hearing = dto };
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<bool> { Success = result, Data = result, Message = "Hearing updated successfully" });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> DeleteHearing(long id)
        {
            var command = new DeleteHearingCommand { HearingId = id };
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<bool> { Success = result, Data = result, Message = "Hearing deleted successfully" });
        }
    }
}