using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(AppDbContext _context, IFileService _fileService, ILogger<UpdateUserCommandHandler> _logger) : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
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
        // 2. Validate RoleID
        // ================================================
        if (!request.RoleID.HasValue)
        {
            _logger.LogWarning("Update failed: RoleID is required");
            throw new ValidationException(["Role is required."]);
        }

        if (!System.Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
        {
            _logger.LogWarning("Update failed: Invalid RoleID: {RoleID}", request.RoleID);
            throw new ValidationException([$"Invalid role. Role ID {request.RoleID} does not exist."]);
        }

        bool roleExists = await _context.Roles
            .AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("Update failed: Role not found: {RoleID}", request.RoleID);
            throw new NotFoundException($"Role with ID {request.RoleID} not found.");
        }

        // ================================================
        // 2b. Enforce role hierarchy + self-protection
        // ================================================
        if (user.UserID == request.ActingUserID && request.RoleID.Value != user.RoleID)
        {
            _logger.LogWarning("User {ActingUserId} attempted to change their own role", request.ActingUserID);
            throw new ValidationException(["You cannot change your own role."]);
        }
        var actingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == request.ActingUserID, cancellationToken);
        var actingRole = actingUser?.GetRole();
        if (actingRole == null || !RoleHierarchy.CanAssignRole(actingRole.Value, request.RoleID.Value))
        {
            _logger.LogWarning("User {ActingUserId} with role {ActingRole} attempted to assign disallowed role {TargetRoleId}",
                request.ActingUserID, actingRole, request.RoleID);
            throw new ValidationException(["You are not authorized to assign this role."]);
        }

        // ================================================
        // 2c. Multi-tenancy: can only edit users in your own firm
        // (SuperAdmin, whose FirmID is null, bypasses this check)
        //
        // NOTE: moved above the email-uniqueness check (previously step 2)
        // so an unauthorized cross-firm request fails fast on an
        // authorization check rather than running an unrelated DB query
        // first. Not a fix for an actual leak — the write was already
        // correctly blocked either way, this is ordering hygiene only.
        // ================================================
        if (actingUser!.FirmID != null && user.FirmID != actingUser.FirmID)
        {
            _logger.LogWarning("User {ActingUserId} attempted to update a user from a different firm: {TargetUserId}", request.ActingUserID, request.UserID);
            throw new ValidationException(["You can only edit users within your own firm."]);
        }

        // ================================================
        // 3. Check if new email is unique
        //    Email is globally unique across all firms by design (see the
        //    unique index on Users.Email in AppDbContext) — login doesn't
        //    ask which firm first, so this check is intentionally NOT
        //    firm-scoped.
        // ================================================
        bool emailExists = await _context.Users
            .AnyAsync(x => x.Email == request.Email && x.UserID != request.UserID, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Update failed: Email already in use: {Email}", request.Email);
            throw new ValidationException(["Email is already in use by another user."]);
        }

        // ================================================
        // 4. Handle profile image update
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
        // 5. Update user properties
        // ================================================
        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Department = request.Department;
        user.RoleID = request.RoleID;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // ================================================
        // 6. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User updated successfully: {UserId}", request.UserID);

        return true;
    }
}