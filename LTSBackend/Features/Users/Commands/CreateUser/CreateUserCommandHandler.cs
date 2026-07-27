using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(AppDbContext _context, IPasswordService _passwordService, IFileService _fileService, IAuditService _auditService, ILogger<CreateUserCommandHandler> _logger) : IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create user request for email: {Email}", request.Email);

        // ================================================
        // 1. Check whether the email already exists
        // ================================================
        bool emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("User creation failed: Email already exists: {Email}", request.Email);
            throw new ValidationException([$"Email '{request.Email}' already exists"]);
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

        bool roleExists = await _context.Roles
            .AsNoTracking()
            .AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("User creation failed: Role not found: {RoleID}", request.RoleID);
            throw new NotFoundException($"Role ID {request.RoleID} not found");
        }

        // ================================================
        // 2b. Enforce role hierarchy — the acting user cannot
        // assign a role above their own or assign SuperAdmin
        // ================================================
        var actingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == request.ActingUserID, cancellationToken);
        var actingRole = actingUser?.GetRole();
        if (actingRole == null || !RoleHierarchy.CanAssignRole(actingRole.Value, request.RoleID.Value))
        {
            _logger.LogWarning("User {ActingUserId} with role {ActingRole} attempted to assign disallowed role {TargetRoleId}", request.ActingUserID, actingRole, request.RoleID);
            throw new ValidationException(["Aap ye role assign karne ke authorized nahi hain."]);
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
            throw new ValidationException(["SuperAdmin naya firm user is endpoint se nahi bana sakta - pehle POST /api/firms se firm banayein."]);
        }

        // ================================================
        // 3. Get role details
        // ================================================
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleID == request.RoleID, cancellationToken);

        _logger.LogInformation("Role fetched: {RoleName}", role?.RoleName);

        // ================================================
        // 4. Validate Department if one was provided
        // ================================================
        if (!string.IsNullOrEmpty(request.Department))
        {
            bool deptExists = await _context.Departments
                .AsNoTracking()
                .AnyAsync(x => x.DepartmentName == request.Department, cancellationToken);

            if (!deptExists)
            {
                _logger.LogInformation("Department nahi mila, user bena department ke banayega");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload profile image for: {Email}", request.Email);
            }
        }

        // ================================================
        // 7. Generate Employee Number
        // ================================================
        string employeeNo = GenerateEmployeeNo();

        // ================================================
        // 8. Create new User
        // ================================================
        var newUser = new User
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
            LastLogin = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created successfully with ID: {UserID} and Role: {RoleName}", newUser.UserID, role?.RoleName);

        // No manual audit log here — the AuditBehavior pipeline already handles this.

        return newUser.UserID;
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