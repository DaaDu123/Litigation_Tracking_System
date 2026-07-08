using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.ResetPassword;

public class ResetPasswordHandler(
    AppDbContext context,
    IPasswordService passwordService,
    IAuditService auditService)
    : IRequestHandler<ResetPasswordCommand, ResetPasswordResponseDTO>
{
    public async Task<ResetPasswordResponseDTO> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var otp = await context.UserOtps
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email &&
                x.OtpCode == request.OtpCode &&
                x.Purpose == OtpPurpose.PasswordReset &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        // FIXED: Use proper List<string> syntax
        if (otp == null)
            throw new ValidationException(new List<string> { "Invalid or expired OTP." });

        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        // FIXED: Use proper List<string> syntax
        if (user == null)
            throw new NotFoundException("User not found.");

        user.PasswordHash = passwordService.HashPassword(request.NewPassword);
        otp.IsUsed = true;

        var auditLog = auditService.Create(user.UserID, "Password Reset");
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponseDTO
        {
            Email = user.Email,
            Message = "Password reset successfully."
        };
    }
}