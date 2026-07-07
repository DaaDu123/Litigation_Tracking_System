
using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Auth.ResendOtp;
using LTSBackend.Models.Security;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

public class ResendOtpHandler (AppDbContext _context, IEmailService _emailService, ILogger<ResendOtpHandler> _logger) : IRequestHandler<ResendOtpCommand, ResendOtpResponseDTO>
{
    public async Task<ResendOtpResponseDTO> Handle(ResendOtpCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resend OTP requested for email: {Email}", request.Email);

        // ================================================
        // 1. Find user by email
        // ================================================
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email,cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Resend OTP failed: User not found for email: {Email}", request.Email);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 2. Remove old unused OTPs
        // ================================================
        var oldOtps = await _context.UserOtps.Where(x => x.Email == request.Email && !x.IsUsed).ToListAsync(cancellationToken);

        if (oldOtps.Count > 0)
        {
            _context.UserOtps.RemoveRange(oldOtps);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} old OTPs for resend", oldOtps.Count);
        }

        // ================================================
        // 3. Generate new 6-digit OTP
        // ================================================
        string otpCode = GenerateSecureOtp();
        _logger.LogInformation("New OTP generated for {Email}", request.Email);

        // ================================================
        // 4. Save new OTP
        // ================================================
        _context.UserOtps.Add(new UserOtp
        {
            Email = request.Email,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            UserID = user.UserID,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("New OTP saved for user: {UserId}", user.UserID);

        // ================================================
        // 5. Send OTP email
        // ================================================
        try
        {
            _logger.LogInformation("Sending OTP email to: {Email}", request.Email);
            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode);
            _logger.LogInformation("OTP email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to: {Email}", request.Email);
            throw;
        }

        return new ResendOtpResponseDTO
        {
            Email = user.Email,
            Message = "OTP sent successfully! Please check your email (including Spam/Junk folder) for the new code."
        };
    }
    private static string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }
}