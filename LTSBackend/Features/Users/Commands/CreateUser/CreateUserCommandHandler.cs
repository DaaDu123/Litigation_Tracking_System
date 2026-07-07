using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LTSBackend.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler (AppDbContext _context, IPasswordService _passwordService, IFileService _fileService, IAuditService _auditService, ILogger<CreateUserCommandHandler> _logger) : IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create user request for email: {Email}", request.Email);

        // ================================================
        // 1. Check agar email pehle se exist karta hai
        // ================================================
        bool emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("User creation failed: Email already exists: {Email}", request.Email);
            throw new ValidationException(new List<string>
            {
                $"Email '{request.Email}' pehle se exist karta hai"
            });
        }

        // ================================================
        // 2. Verify ke Role valid hai aur exist karta hai
        // ================================================
        if (!request.RoleID.HasValue || request.RoleID <= 0)
        {
            _logger.LogWarning("User creation failed: Invalid RoleID");
            throw new ValidationException(new List<string>
            {
                "Valid Role zaroori hai"
            });
        }

        // Enum mein check kare
        if (!Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
        {
            _logger.LogWarning("User creation failed: Invalid RoleID: {RoleID}", request.RoleID);
            throw new ValidationException(new List<string>
            {
                $"Invalid role. Role ID {request.RoleID} exist nahi karta"
            });
        }

        // Database mein verify kare ke Role exist karta hai
        bool roleExists = await _context.Roles
            .AsNoTracking()
            .AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("User creation failed: Role not found: {RoleID}", request.RoleID);
            throw new NotFoundException($"Role ID {request.RoleID} nahi mila");
        }

        // ================================================
        // 3. Get role details
        // ================================================
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleID == request.RoleID, cancellationToken);

        _logger.LogInformation("Role fetched: {RoleName}", role?.RoleName);

        // ================================================
        // 4. Validate Department agar diya gaya hai
        // ================================================
        if (!string.IsNullOrEmpty(request.Department))
        {
            bool deptExists = await _context.Departments
                .AsNoTracking()
                .AnyAsync(x => x.DepartmentName == request.Department, cancellationToken);

            if (!deptExists)
            {
                _logger.LogWarning("Department not found: {Department}", request.Department);
                // Department auto-create na kare, sirf warning de
                _logger.LogInformation("Department nahi mila, user bena department ke banayega");
            }
        }

        // ================================================
        // 5. Hash the password
        // ================================================
        string passwordHash = _passwordService.HashPassword(request.Password);
        _logger.LogInformation("Password hashed for email: {Email}", request.Email);

        // ================================================
        // 6. Handle profile image upload
        // ================================================
        string? profileImagePath = null;
        if (request.ProfileImage != null && request.ProfileImage.Length > 0)
        {
            try
            {
                profileImagePath = await _fileService.SaveFileAsync(
                    request.ProfileImage,
                    "profile_pictures");

                _logger.LogInformation("Profile image uploaded for: {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload profile image for: {Email}", request.Email);
                // Profile image fail ho to user creation fail na kare
                // Sirf warning de aur continue kare
            }
        }

        // ================================================
        // 7. Generate Employee Number
        // ================================================
        string employeeNo = GenerateEmployeeNo();
        _logger.LogInformation("Generated employee number: {EmployeeNo}", employeeNo);

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
            Designation = null, // TODO: Add designation se request
            ProfileImage = profileImagePath,
            RoleID = request.RoleID.Value,
            IsExternal = false,
            IsActive = true, // Default active
            IsDeleted = false,
            LastLogin = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created successfully with ID: {UserID} and Role: {RoleName}",
            newUser.UserID, role?.RoleName);

        // ================================================
        // 9. Create Audit Log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(newUser.UserID, $"User Created: {newUser.Email}"));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit log created for new user: {UserID}", newUser.UserID);

        return newUser.UserID;
    }

    /// <summary>
    /// Generate unique Employee Number
    /// Format: EMP-20240115-5D8K
    /// </summary>
    private static string GenerateEmployeeNo()
    {
        var now = DateTime.UtcNow;
        var randomPart = GenerateRandomString(4);
        return $"EMP-{now:yyyyMMdd}-{randomPart}";
    }

    /// <summary>
    /// Generate random alphanumeric string
    /// </summary>
    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}