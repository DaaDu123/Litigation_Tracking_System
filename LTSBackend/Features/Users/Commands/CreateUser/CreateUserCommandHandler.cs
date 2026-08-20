using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(AppDbContext _context, IPasswordService _passwordService, IFileService _fileService, ILogger<CreateUserCommandHandler> _logger) : IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create user request for email: {Email}", request.Email);

        // ================================================
        // 0. Load the acting user first (needed both for the
        // role-hierarchy checks below AND for the email-reuse
        // ownership check in step 1 - we need to know the acting
        // firm before we can decide whether a soft-deleted row with
        // this email belongs to "us" or to another firm).
        // ================================================
        var actingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == request.ActingUserID, cancellationToken);
        if (actingUser == null)
        {
            _logger.LogWarning("User creation failed: acting user not found: {ActingUserId}", request.ActingUserID);
            throw new ValidationException(["Could not identify the requesting user."]);
        }

        // ================================================
        // 2c. Multi-tenancy: new user inherits the acting user's
        // firm. SuperAdmin has no firm of their own, so they can't
        // create firm-scoped users via this endpoint - firms are
        // bootstrapped (with their first Firm Admin) via
        // POST /api/firms instead.
        // ================================================
        if (actingUser.FirmID == null)
        {
            _logger.LogWarning("SuperAdmin {ActingUserId} attempted to create a user via /api/users instead of /api/firms", request.ActingUserID);
            throw new ValidationException(["SuperAdmin cannot create a firm user via this endpoint - create the firm first via POST /api/firms."]);
        }

        // ================================================
        // 1. Check whether the email already exists, and whether a
        // soft-deleted row for it can be reused.
       // ================================================
        var existingUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        bool isReuse = false;

        if (existingUser != null)
        {
            if (!existingUser.IsDeleted)
            {
                // Live user already owns this email - own firm or another
                // firm, doesn't matter, nobody else can take it.
                _logger.LogWarning("User creation failed: Email already exists and is active: {Email}", request.Email);
                throw new ValidationException([$"Email '{request.Email}' already exists and is currently assigned to another user."]);
            }

            // existingUser.IsDeleted == true: a soft-deleted record for
            // this email exists somewhere. Ownership rule: FirmID is left
            // untouched on delete, so it still tells us who "owns" the
            // reservation.
            bool sameFirm = existingUser.FirmID == actingUser.FirmID;

            if (!sameFirm && !existingUser.IsReleasedForReuse)
            {
                _logger.LogWarning("User creation failed: Email {Email} is a deleted record owned by firm {OwnerFirmId}, not released, requested by firm {RequestingFirmId}",request.Email, existingUser.FirmID, actingUser.FirmID);
                throw new ValidationException([$"Email '{request.Email}' already exists and is reserved by another firm's deleted user record. " + "It must be released for reassignment before it can be reused here."]);
            }

            // Either the same firm reclaiming its own deleted user, or a
            // different firm reusing a record that was explicitly released.
            isReuse = true;
        }

        // ================================================
        // 2. Verify that the Role is valid and exists
        // ================================================
        if (!request.RoleID.HasValue || request.RoleID <= 0)
        {
            _logger.LogWarning("User creation failed: Invalid RoleID");
            throw new ValidationException(["A valid Role is required"]);
        }

        if (!System.Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
        {
            _logger.LogWarning("User creation failed: Invalid RoleID: {RoleID}", request.RoleID);
            throw new ValidationException([$"Invalid role. Role ID {request.RoleID} does not exist"]);
        }

        bool roleExists = await _context.Roles.AsNoTracking().AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("User creation failed: Role not found: {RoleID}", request.RoleID);
            throw new NotFoundException($"Role ID {request.RoleID} not found");
        }

        // ================================================
        // 2b. Enforce role hierarchy — the acting user cannot
        // assign a role above their own or assign SuperAdmin
        // ================================================
        var actingRole = actingUser.GetRole();
        if (actingRole == null || !RoleHierarchy.CanAssignRole(actingRole.Value, request.RoleID.Value))
        {
            _logger.LogWarning("User {ActingUserId} with role {ActingRole} attempted to assign disallowed role {TargetRoleId}", request.ActingUserID, actingRole, request.RoleID);
            throw new ValidationException(["You are not authorized to assign this role."]);
        }

        // ================================================
        // 3. Get role details
        // ================================================
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.RoleID == request.RoleID, cancellationToken);

        _logger.LogInformation("Role fetched: {RoleName}", role?.RoleName);

        // ================================================
        // 4. Validate Department if one was provided
        // ================================================
        if (!string.IsNullOrEmpty(request.Department))
        {
            bool deptExists = await _context.Departments.AsNoTracking().AnyAsync(x => x.DepartmentName == request.Department, cancellationToken);

            if (!deptExists)
            {
                _logger.LogInformation("Department not found, user will be created without a department");
            }
        }

        // ================================================
        // 5. Hash the password
        // ================================================
        string passwordHash = _passwordService.HashPassword(request.Password);

        // ================================================
        // 6. Handle profile image upload
        // ================================================
        string? profileImagePath = null;
        if (request.ProfileImage is { Length: > 0 })
        {
            try
            {
                profileImagePath = await _fileService.SaveFileAsync(request.ProfileImage, "profile_pictures");
                _logger.LogInformation("Profile image uploaded for: {Email}", request.Email);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload profile image for: {Email}", request.Email);
            }
        }

        // ================================================
        // 7/8. Either reuse the existing soft-deleted record, or
        // create a brand new User row.
        // ================================================
        int resultUserId;
        User? newUser = null;

        if (isReuse)
        {
            // ============================================
            // REUSE PATH — restore the original row instead of
            // inserting a new one. Same UserID, same EmployeeNo,
            // same historical CreatedAt/audit trail; everything
            // else is overwritten with the new assignment's data.
            // Must re-fetch WITH tracking + IgnoreQueryFilters(),
            // since the earlier lookup was a fresh query and the
            // row is invisible to normal tracked queries while
            // IsDeleted == true.
            // ================================================
            var userToRestore = await _context.Users.IgnoreQueryFilters().FirstAsync(x => x.Email == request.Email, cancellationToken);

            _logger.LogInformation("Reusing soft-deleted user record {UserID} (previously firm {PreviousFirmId}) for email {Email}, new firm {NewFirmId}",userToRestore.UserID, userToRestore.FirmID, request.Email, actingUser.FirmID);

            userToRestore.FullName = request.FullName;
            userToRestore.PasswordHash = passwordHash;
            userToRestore.Phone = request.Phone;
            userToRestore.Department = request.Department;
            userToRestore.Designation = null;
            userToRestore.ProfileImage = profileImagePath;
            userToRestore.RoleID = request.RoleID.Value;
            userToRestore.FirmID = actingUser.FirmID;
            userToRestore.IsExternal = false;
            userToRestore.IsActive = true;
            userToRestore.IsDeleted = false;
            userToRestore.IsReleasedForReuse = false; // reservation consumed
            userToRestore.FailedLoginAttempts = 0;
            userToRestore.LastLogin = null;
            userToRestore.PasswordChangedDate = DateTime.UtcNow;
            userToRestore.SecurityStamp = Guid.NewGuid().ToString("N"); // invalidate any stale tokens
            userToRestore.UpdatedAt = DateTime.UtcNow;
            // EmployeeNo, UserID and CreatedAt deliberately untouched —
            // this is still, by identity, the same record/person history.

            resultUserId = userToRestore.UserID;
        }
        else
        {
            // ============================================
            // NORMAL PATH — brand new person, brand new row.
            // ============================================
            string employeeNo = GenerateEmployeeNo();

            newUser = new User
            {
                EmployeeNo = employeeNo,
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                Phone = request.Phone,
                Department = request.Department,
                Designation = null,
                ProfileImage = profileImagePath,
                RoleID = request.RoleID.Value,
                FirmID = actingUser.FirmID,
                IsExternal = false,
                IsActive = true,
                IsDeleted = false,
                IsReleasedForReuse = false,
                LastLogin = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Users.Add(newUser);
            resultUserId = 0; // populated after SaveChanges below
        }

        // ================================================
        // 9. Persist. Race-condition guard: two concurrent requests
        // could both pass the email-existence check above before
        // either commits (classic TOCTOU). The filtered unique index
        // on Users.Email is the real backstop - catch its violation
        // here and turn it into the same clean validation message
        // instead of an unhandled 500.
        // ================================================
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogWarning(ex, "Concurrent create/reuse race detected for email: {Email}", request.Email);
            throw new ValidationException([$"Email '{request.Email}' was just claimed by another request. Please try again."]);
        }

        if (!isReuse && newUser != null)
        {
            // SaveChanges populates the identity value straight onto the
            // tracked entity — no need for a second round-trip.
            resultUserId = newUser.UserID;
        }

        _logger.LogInformation("User {Action} successfully with ID: {UserID} and Role: {RoleName}", isReuse ? "restored/reused" : "created", resultUserId, role?.RoleName);

        // No manual audit log here — the AuditBehavior pipeline already handles this.

        return resultUserId;
    }

    private static string GenerateEmployeeNo()
    {
        var now = DateTime.UtcNow;
        var randomPart = GenerateRandomString(4);
        return $"EMP-{now:yyyyMMdd}-{randomPart}";
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}