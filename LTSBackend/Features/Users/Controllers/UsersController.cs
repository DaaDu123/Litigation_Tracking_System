using LTSBackend.Comman.Responses;
using LTSBackend.Features.Users.Commands.CreateUser;
using LTSBackend.Features.Users.Commands.DeleteUser;
using LTSBackend.Features.Users.Commands.UpdateUser;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetAllUsers;
using LTSBackend.Features.Users.Queries.GetUserById;
using LTSBackend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.Users.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IMediator mediator) : ControllerBase
{
    // =========================
    // CREATE USER
    // =========================
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOnly)]
    public async Task<IActionResult> Create(
        [FromForm] CreateUserCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "User created successfully."));
    }

    // =========================
    // GET ALL USERS
    // =========================
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Clerk)]
    public async Task<IActionResult> GetAll()
    {
        var users = await mediator.Send(new GetAllUsersQuery());
        return Ok(ApiResponse<List<UserDTO>>.SuccessResponse(users, "Users fetched successfully."));
    }

    // =========================
    // GET USER BY ID
    // =========================
    [HttpGet("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Clerk)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await mediator.Send(new GetUserByIdQuery(id));
        return Ok(ApiResponse<UserDTO>.SuccessResponse(user!, "User fetched successfully."));
    }

    // =========================
    // UPDATE USER
    // =========================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.AdminOnly)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateUserCommand command)
    {
        if (id != command.UserID)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and UserID mismatch."));
        }
        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "User updated successfully."));
    }

    // =========================
    // SOFT DELETE USER
    // =========================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteUserCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "User deactivated successfully."));
    }
}
