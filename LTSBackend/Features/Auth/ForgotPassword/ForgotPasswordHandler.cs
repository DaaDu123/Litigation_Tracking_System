using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LTSBackend.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler(AppDbContext _context, IEmailService _emailService, ILogger<ForgotPasswordHandler> _logger) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseDTO>
{
    public async Task<ForgotPasswordResponseDTO> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Forgot password requested for email: {Email}", request.Email);

        // ================================================
        // Generic response - we always return this regardless of whether
        // the email exists or not. This protects against "user enumeration"
        // attacks (so no one can guess which email is
        // registered).
        // ================================================
        var genericResponse = new ForgotPasswordResponseDTO
        {
            Email = request.Email,
            Message = "If this email is registered in our system, an OTP code has been sent. Please also check your spam/junk folder."
        };

        // ================================================
        // 1. Look up the user - even if not found, still return the generic response
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == request.Email && !x.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Forgot password: No user found for {Email} (returning a generic response)",request.Email);
            return genericResponse;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Forgot password: User is inactive: {Email} (returning generic response)",request.Email);
            return genericResponse;
        }

        // ================================================
        // 2. Remove old unused PasswordReset OTPs
        //    (do not touch OTPs for the Registration purpose)
        // ================================================
        var oldOtps = await _context.UserOtps
            .Where(x =>
                x.Email == request.Email &&
                !x.IsUsed &&
                x.Purpose == OtpPurpose.PasswordReset)
            .ToListAsync(cancellationToken);

        if (oldOtps.Count > 0)
        {
            _context.UserOtps.RemoveRange(oldOtps);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Removed {Count} old password-reset OTPs for {Email}",
                oldOtps.Count,
                request.Email);
        }

        // ================================================
        // 3. Nayi OTP generate aur save kare (Purpose = PasswordReset)
        // ================================================
        string otpCode = GenerateSecureOtp();

        _context.UserOtps.Add(new UserOtp
        {
            Email = request.Email,
            OtpCode = otpCode,
            Purpose = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            UserID = user.UserID,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password-reset OTP generated for user: {UserId}", user.UserID);

        // ================================================
        // 4. Email bhejain - fail ho jaye to bhi generic response
        //    hi wapas jaye (taake attacker ko pata na chale)
        // ================================================
        try
        {
            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode);
            _logger.LogInformation("Password-reset OTP email sent to: {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset OTP email to: {Email}", request.Email);
            // Don't throw - for security we keep returning the generic response
        }

        return genericResponse;
    }

    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }
}