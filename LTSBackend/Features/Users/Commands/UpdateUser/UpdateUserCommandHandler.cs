using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        AppDbContext context,
        IFileService fileService,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _context = context;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", request.UserID);

        // ================================================
        // 1. Find user
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Update failed: User not found: {UserId}", request.UserID);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 2. Check if new email is unique
        // ================================================
        bool emailExists = await _context.Users
            .AnyAsync(x =>
                x.Email == request.Email &&
                x.UserID != request.UserID,
                cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Update failed: Email already in use: {Email}", request.Email);
            throw new ValidationException(new List<string> { "Email is already in use by another user." });
        }

        // ================================================
        // 3. Validate RoleID
        // ================================================
        if (!request.RoleID.HasValue)
        {
            _logger.LogWarning("Update failed: RoleID is required");
            throw new ValidationException(new List<string> { "Role is required." });
        }

        if (!Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
        {
            _logger.LogWarning("Update failed: Invalid RoleID: {RoleID}", request.RoleID);
            throw new ValidationException(
                new List<string> { $"Invalid role. Role ID {request.RoleID} does not exist." });
        }

        // ================================================
        // 4. Verify role exists in database
        // ================================================
        bool roleExists = await _context.Roles
            .AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("Update failed: Role not found: {RoleID}", request.RoleID);
            throw new NotFoundException($"Role with ID {request.RoleID} not found.");
        }

        // ================================================
        // 5. Handle profile image update
        // ================================================
        if (request.ProfileImage != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                _fileService.DeleteFile(user.ProfileImage);
                _logger.LogInformation("Old profile image deleted for user: {UserId}", request.UserID);
            }

            // Upload new image
            user.ProfileImage = await _fileService.SaveFileAsync(request.ProfileImage, "profile_pictures");
            _logger.LogInformation("New profile image saved for user: {UserId}", request.UserID);
        }

        // ================================================
        // 6. Update user properties
        // ================================================
        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Department = request.Department;
        user.RoleID = request.RoleID;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // ================================================
        // 7. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User updated successfully: {UserId}", request.UserID);

        return true;
    }
}