using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LTSBackend.Features.Auth.Register;

public class RegisterHandler(AppDbContext context, IPasswordService passwordService, IEmailService emailService,
    IAuditService auditService, ILogger<RegisterHandler> logger) : IRequestHandler<RegisterCommand, RegisterResponseDTO>
{
    public async Task<RegisterResponseDTO> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting registration for email: {Email}", request.Email);

        // 1. Check Email
        bool emailExists = await context.Users.AsNoTracking().AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            logger.LogWarning("Email already exists: {Email}", request.Email);
            throw new ValidationException(new List<string> { "Email already exists." });
        }

        // ================================================
        // 2. Get default role (InternParalegal)
        // ================================================
        var defaultRole = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RoleID == (int)UserRole.InternParalegal,
                cancellationToken);

        if (defaultRole == null)
        {
            _logger.LogError("Default InternParalegal role not found in database");
            throw new NotFoundException("Default role not found. Please contact administrator.");
        }

        // 3. Create User
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordService.HashPassword(request.Password),
            Phone = request.Phone,
            Department = request.Department,
            RoleID = defaultRole.RoleID,
            IsActive = false,  // Inactive until email verified
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            EmployeeNo = GenerateEmployeeNo()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User created with ID: {UserId}", user.UserID);

        // 4. Remove Old OTPs
        var oldOtps = await context.UserOtps.Where(x => x.Email == request.Email && !x.IsUsed).ToListAsync(cancellationToken);

        // ================================================
        // 4. Clean up old unused Registration OTPs
        // ================================================
        var oldOtps = await _context.UserOtps
            .Where(x => x.Email == request.Email && !x.IsUsed && x.Purpose == OtpPurpose.Registration)
            .ToListAsync(cancellationToken);

        if (oldOtps.Count > 0)
        {
            context.UserOtps.RemoveRange(oldOtps);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Removed {Count} old OTPs for {Email}", oldOtps.Count, request.Email);
        }

        // 5. Generate OTP
        string otpCode = GenerateSecureOtp();
        // SECURITY: never log the raw OTP value.
        logger.LogInformation("OTP generated for {Email}", request.Email);

        // ================================================
        // 6. Save OTP with expiry (Purpose = Registration)
        // ================================================
        var userOtp = new UserOtp
        {
            Email = request.Email,
            OtpCode = otpCode,
            Purpose = OtpPurpose.Registration,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            UserID = user.UserID,
            CreatedAt = DateTime.UtcNow
        };

        context.UserOtps.Add(userOtp);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("OTP saved for user: {UserId}", user.UserID);

        // 7. Audit Log
        var auditLog = auditService.Create(user.UserID, "User Registered");
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);

        // 8. Send OTP Email
        try
        {
            logger.LogInformation("Sending OTP email to: {Email}", request.Email);
            await emailService.SendOtpEmailAsync(
                request.Email,
                request.FullName,
                otpCode);
            logger.LogInformation("OTP email sent successfully to: {Email}", request.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to: {Email}", request.Email);
            // Don't throw - user can resend OTP
        }

        return new RegisterResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            // SECURITY: OTP is never returned in the API response, only emailed.
            // Gmail and other providers sometimes route automated/transactional
            // mail to Spam on first contact with a sender — tell the user to check
            // there so they aren't stuck thinking nothing was sent.
            Message = "Registration successful. Please check your email (including the Spam/Junk folder) for the OTP code."
        };
    }

    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }
}