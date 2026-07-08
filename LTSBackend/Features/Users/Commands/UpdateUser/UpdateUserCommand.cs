using FluentValidation;
using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.AspNetCore.Http;
namespace LTSBackend.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    int UserID,
    string FullName,
    string Email,
    string? Phone,
    string? Department,
    int? RoleID,
    bool IsActive,
    IFormFile? ProfileImage
) : IRequest<bool>;

/// <summary>
/// Validator for update user command.
/// </summary>


/// <summary>
/// Handler for updating user information.
/// Can change role, contact info, profile image, and active status.
/// </summary>
