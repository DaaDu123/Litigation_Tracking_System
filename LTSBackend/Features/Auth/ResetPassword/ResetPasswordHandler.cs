using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Auth.ResetPassword;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponseDTO>
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        AppDbContext context,
        IPasswordService passwordService,
        IAuditService auditService,
        ILogger<ResetPasswordHandler> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ResetPasswordResponseDTO> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

        // ================================================
        // 1. Find and validate OTP
        // ================================================
        var otp = await _context.UserOtps
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email &&
                x.OtpCode == request.OtpCode &&
                x.Purpose == OtpPurpose.PasswordReset &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (otp == null)
        {
            _logger.LogWarning("Password reset failed: Invalid or expired OTP for email: {Email}", request.Email);
            throw new ValidationException(
                new List<string> { "Invalid or expired OTP code." });
        }

        // ================================================
        // 2. Find user
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == request.Email,
                cancellationToken);

        if (user == null)
        {
            _logger.LogError("Password reset failed: User not found for email: {Email}", request.Email);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 3. Update password
        // ================================================
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);

        // ================================================
        // 4. Mark OTP as used
        // ================================================
        otp.IsUsed = true;

        // ================================================
        // 5. Create audit log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(user.UserID, "Password Reset via OTP"));

        // ================================================
        // 6. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for user: {UserId}", user.UserID);

        return new ResetPasswordResponseDTO
        {
            Email = user.Email,
            Message = "Password reset successfully! You can now login with your new password."
        };
    }
}