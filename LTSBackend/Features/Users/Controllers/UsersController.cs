using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Users.Commands.CreateUser;
using LTSBackend.Features.Users.Commands.DeleteUser;
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
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // =====================================================
    // CREATE USER
    // =====================================================
    /// <summary>
    /// Naya user create kare
    /// Role-based: SuperAdmin, FirmAdmin, Partner
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> Create([FromForm] CreateUserCommand command)
    {
        _logger.LogInformation("Create user request for email: {Email}", command.Email);

        var id = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            ApiResponse<int>.SuccessResponse(
                id,
                "User successfully created"));
    }

    // =====================================================
    // GET ALL USERS
    // =====================================================
    /// <summary>
    /// Tamam users fetch kare
    /// Role-based: FirmAdmin, Partner can view all
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Get all users request");

        var users = await _mediator.Send(new GetAllUsersQuery());

        return Ok(ApiResponse<List<UserDTO>>.SuccessResponse(
            users,
            "Users successfully fetched"));
    }

    // =====================================================
    // GET USER BY ID
    // =====================================================
    /// <summary>
    /// Aik specific user fetch kare.
    /// Role-based: sirf PartnerAndAbove roles access kar sakte hain.
    /// Self-view ke liye separate "/profile/me" endpoint use karo —
    /// ye endpoint self-access exception nahi deta.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Get user request: {UserID}", id);

        // ✅ FIX: pehle yahan currentUserId nikal ke discard ho raha tha
        // (dead code) — comment kehta tha "self-view allowed" lekin
        // [Authorize(Roles=PartnerAndAbove)] pehle hi non-partner roles
        // ko block kar deta hai, is liye wo code kabhi meaningful nahi tha.
        // TODO: Partner role ko sirf apne firm ke users tak restrict
        // karna hai jab multi-tenant support add ho.

        var user = await _mediator.Send(new GetUserByIdQuery(id));

        if (user == null)
        {
            return NotFound(ApiResponse<UserDTO>.FailureResponse("User nahi mila"));
        }

        return Ok(ApiResponse<UserDTO>.SuccessResponse(
            user,
            "User successfully fetched"));
    }

    // =====================================================
    // UPDATE USER
    // =====================================================
    /// <summary>
    /// User update kare
    /// Role-based: SuperAdmin, FirmAdmin only
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateUserCommand command)
    {
        _logger.LogInformation("Update user request: {UserID}", id);

        if (id != command.UserID)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(
                "URL aur body mein user ID match nahi hain"));
        }

        var result = await _mediator.Send(command);

        return Ok(ApiResponse<bool>.SuccessResponse(
            result,
            "User successfully updated"));
    }

    // =====================================================
    // DELETE USER (Soft Delete)
    // =====================================================
    /// <summary>
    /// User deactivate kare (soft delete)
    /// Role-based: SuperAdmin, FirmAdmin only
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete user request: {UserID}", id);

        var result = await _mediator.Send(new DeleteUserCommand(id));

        return Ok(ApiResponse<bool>.SuccessResponse(
            result,
            "User successfully deactivated"));
    }

    // =====================================================
    // GET MY PROFILE
    // =====================================================
    /// <summary>
    /// Apna profile dekhe
    /// Everyone can access
    /// </summary>
    [HttpGet("profile/me")]
    public async Task<IActionResult> GetMyProfile()
    {
        _logger.LogInformation("Get my profile request");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse(
                "Invalid or missing user identity"));
        }

        var user = await _mediator.Send(new GetUserByIdQuery(userId));

        if (user == null)
        {
            return NotFound(ApiResponse<UserDTO>.FailureResponse("User nahi mila"));
        }

        return Ok(ApiResponse<UserDTO>.SuccessResponse(
            user,
            "Profile successfully fetched"));
    }
}