using System.Security.Claims;
using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.LoginHistory.Commands.DeleteLoginHistory;
using LTSBackend.Features.LoginHistory.Commands.DeleteOldHistory;
using LTSBackend.Features.LoginHistory.DTOs;
using LTSBackend.Features.LoginHistory.Queries.GetAllLoginHistory;
using LTSBackend.Features.LoginHistory.Queries.GetMyLoginHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.LoginHistory.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission("ViewLoginHistory")]
public class LoginHistoryController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await mediator.Send(new GetAllLoginHistoryQuery(search, fromDate, toDate, status, pageNumber, pageSize));
        return Ok(ApiResponse<PagedResult<LoginHistoryDTO>>.SuccessResponse(result, "Login history fetched successfully."));
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyHistory()
    {
        // FIX: previously used `User.FindFirstValue(ClaimTypes.NameIdentifier)!`
        // which suppresses the null warning but throws an unhandled
        // ArgumentNullException at runtime (500 error) if the claim is
        // ever missing. Now handled gracefully with a proper 401.
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity"));
        }

        var result = await mediator.Send(new GetMyLoginHistoryQuery(userId));

        return Ok(ApiResponse<List<MyLoginHistoryDTO>>.SuccessResponse(result, "My login history fetched successfully."));
    }

    [HttpDelete("{id:int}")]
    [HasPermission("DeleteLoginHistory")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteLoginHistoryCommand(id));

        return Ok(ApiResponse<bool>.SuccessResponse(result, "Login history deleted successfully."));
    }

    [HttpDelete("cleanup")]
    [HasPermission("DeleteLoginHistory")]
    public async Task<IActionResult> Cleanup([FromQuery] int days = 90)
    {
        var deleted = await mediator.Send(new DeleteOldHistoryCommand(days));

        return Ok(ApiResponse<int>.SuccessResponse(deleted, $"{deleted} records deleted."));
    }
}