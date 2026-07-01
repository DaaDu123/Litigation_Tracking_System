using System.Security.Claims;
using LTSBackend.Comman.Responses;
using LTSBackend.Features.Profile.Commands;
using LTSBackend.Features.Profile.DTOs;
using LTSBackend.Features.Profile.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Profile.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfileController(IMediator mediator): ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim =User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity."));
        }

        var profile = await mediator.Send( new GetMyProfileQuery(userId));

        return Ok(ApiResponse<ProfileDTO>.SuccessResponse(profile,"Profile fetched successfully."));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateMyProfileCommand command)
    {
        var userIdClaim =User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity."));
        }

        var request = command with { UserID = userId };

        var result = await mediator.Send(request);

        return Ok(ApiResponse<bool>.SuccessResponse(result,"Profile updated successfully."));
    }
}