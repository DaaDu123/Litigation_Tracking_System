using LTSBackend.Comman.Responses;
using LTSBackend.Features.Users.Commands.ActivateUser;
using LTSBackend.Features.Users.Commands.CreateUser;
using LTSBackend.Features.Users.Commands.DeleteUser;
using LTSBackend.Features.Users.Commands.PermanentDeleteUser;
using LTSBackend.Features.Users.Commands.UpdateUser;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetAllUsers;
using LTSBackend.Features.Users.Queries.GetUserById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LTSBackend.Features.Users.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController(IMediator _mediator, ILogger<UsersController> _logger) : ControllerBase
{
    // =====================================================
    // CREATE USER — FirmAdmin + SuperAdmin
    // =====================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create([FromForm] CreateUserCommand command)
    {
        var actingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actingUserIdClaim, out var actingUserId))
            return Unauthorized(ApiResponse<int>.FailureResponse("Invalid identity."));

        var id = await _mediator.Send(command with { ActingUserID = actingUserId });

        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<int>.SuccessResponse(id, "User successfully created"));
    }

    // =====================================================
    // GET ALL USERS
    // =====================================================
    [HttpGet]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Get all users request");

        var users = await _mediator.Send(new GetAllUsersQuery());

        return Ok(ApiResponse<List<UserDTO>>.SuccessResponse(users, "Users successfully fetched"));
    }

    // =====================================================
    // GET USER BY ID
    // =====================================================
    [HttpGet("{id}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Get user request: {UserID}", id);

        var user = await _mediator.Send(new GetUserByIdQuery(id));

        if (user == null)
            return NotFound(ApiResponse<UserDTO>.FailureResponse("User not found"));

        return Ok(ApiResponse<UserDTO>.SuccessResponse(user, "User successfully fetched"));
    }

    // =====================================================
    // UPDATE USER — FirmAdmin only
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateUserCommand command)
    {
        _logger.LogInformation("Update user request: {UserID}", id);

        if (id != command.UserID)
            return BadRequest(ApiResponse<bool>.FailureResponse("URL and body user ID do not match"));

        var actingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actingUserIdClaim, out var actingUserId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid identity."));

        var result = await _mediator.Send(command with { ActingUserID = actingUserId });

        return Ok(ApiResponse<bool>.SuccessResponse(result, "User successfully updated"));
    }

    // =====================================================
    // DELETE USER (Deactivate — reversible) — FirmAdmin only
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deactivate user request: {UserID}", id);

        var actingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actingUserIdClaim, out var actingUserId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid identity."));

        var result = await _mediator.Send(new DeleteUserCommand(id) { ActingUserID = actingUserId });

        return Ok(ApiResponse<bool>.SuccessResponse(result, "User successfully deactivated"));
    }

    // =====================================================
    // ACTIVATE USER (reverses Deactivate) — FirmAdmin only
    // =====================================================
    [HttpPut("{id}/activate")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Activate(int id)
    {
        _logger.LogInformation("Activate user request: {UserID}", id);

        var actingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actingUserIdClaim, out var actingUserId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid identity."));

        var result = await _mediator.Send(new ActivateUserCommand(id) { ActingUserID = actingUserId });

        return Ok(ApiResponse<bool>.SuccessResponse(result, "User successfully activated"));
    }

    // =====================================================
    // PERMANENT DELETE — FirmAdmin only
    // =====================================================
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        _logger.LogInformation("Permanent delete user request: {UserID}", id);

        var actingUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actingUserIdClaim, out var actingUserId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid identity."));

        var result = await _mediator.Send(new PermanentDeleteUserCommand(id) { ActingUserID = actingUserId });

        return Ok(ApiResponse<bool>.SuccessResponse(result, "User permanently deleted"));
    }

    // =====================================================
    // GET MY PROFILE
    // =====================================================
    [HttpGet("profile/me")]
    public async Task<IActionResult> GetMyProfile()
    {
        _logger.LogInformation("Get my profile request");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user identity"));

        var user = await _mediator.Send(new GetUserByIdQuery(userId));

        if (user == null)
        {
            return NotFound(ApiResponse<UserDTO>.FailureResponse("User not found"));
        }

        return Ok(ApiResponse<UserDTO>.SuccessResponse(user, "Profile successfully fetched"));
    }
}