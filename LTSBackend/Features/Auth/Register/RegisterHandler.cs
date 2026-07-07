using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LTSBackend.Features.Auth.Register;
public class RegisterHandler(AppDbContext _context, IPasswordService _passwordService, IEmailService _emailService, IAuditService _auditService, ILogger<RegisterHandler> _logger) : IRequestHandler<RegisterCommand, RegisterResponseDTO>
{

    public async Task<RegisterResponseDTO> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting registration for email: {Email}", request.Email);

        // ================================================
        // 1. Check if email already exists
        // ================================================
        bool emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Registration failed: Email already exists: {Email}", request.Email);
            throw new ValidationException(new List<string> { "Email already exists." });
        }

        // ================================================
        // 2. Get default role (Operator)
        // ================================================
        var defaultRole = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RoleID == (int)UserRole.Operator,
                cancellationToken);

        if (defaultRole == null)
        {
            _logger.LogError("Default Operator role not found in database");
            throw new NotFoundException("Default role not found. Please contact administrator.");
        }

        // ================================================
        // 3. Create user account
        // ================================================
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Phone = request.Phone,
            Department = request.Department,
            RoleID = defaultRole.RoleID,
            IsActive = false,  // Inactive until email verified
            CreatedAt = DateTime.UtcNow,
            EmployeeNo = GenerateEmployeeNo()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created successfully with ID: {UserId}", user.UserID);

        // ================================================
        // 4. Clean up old unused OTPs
        // ================================================
        var oldOtps = await _context.UserOtps
            .Where(x => x.Email == request.Email && !x.IsUsed)
            .ToListAsync(cancellationToken);

        if (oldOtps.Count > 0)
        {
            _context.UserOtps.RemoveRange(oldOtps);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} old OTPs for {Email}", oldOtps.Count, request.Email);
        }

        // ================================================
        // 5. Generate 6-digit OTP
        // ================================================
        string otpCode = GenerateSecureOtp();
        _logger.LogInformation("OTP generated for {Email}", request.Email);

        // ================================================
        // 6. Save OTP with expiry
        // ================================================
        var userOtp = new UserOtp
        {
            Email = request.Email,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            UserID = user.UserID,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserOtps.Add(userOtp);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OTP saved for user: {UserId}", user.UserID);

        // ================================================
        // 7. Create audit log
        // ================================================
        var auditLog = _auditService.Create(user.UserID, "User Registered");
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        // ================================================
        // 8. Send OTP email
        // ================================================
        try
        {
            _logger.LogInformation("Sending OTP email to: {Email}", request.Email);
            await _emailService.SendOtpEmailAsync(request.Email, request.FullName, otpCode);
            _logger.LogInformation("OTP email sent successfully to: {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to: {Email}", request.Email);
            // Don't throw — user can use ResendOtp endpoint if email fails
        }

        return new RegisterResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Message = "Registration successful! Please check your email (including Spam/Junk folder) for the OTP code to verify your account."
        };
    }
    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }
    private static string GenerateEmployeeNo()
    {
        var now = DateTime.UtcNow;
        var random = RandomNumberGenerator.GetInt32(1000, 9999);
        return $"EMP-{now:yyyyMMdd}-{random}";
    }
}