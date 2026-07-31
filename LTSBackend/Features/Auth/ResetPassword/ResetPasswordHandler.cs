using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<ResetPasswordResponseDTO> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt via reset link");

        // ================================================
        // 1. Hash the incoming token and look it up directly - we never
        //    trust an email supplied by the client for this flow; the
        //    token alone (and only the token) identifies the account.
        // ================================================
        string tokenHash = HashToken(request.Token);

        var resetToken = await _context.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (resetToken == null || resetToken.User == null)
        {
            _logger.LogWarning("Password reset failed: invalid, expired, or already-used reset link");
            throw new ValidationException(new List<string> { "This reset link is invalid or has expired. Please request a new one." });
        }

        var user = resetToken.User;

        // ================================================
        // 2. Update password
        // ================================================
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // ================================================
        // 3. Mark token as used (single-use link)
        // ================================================
        resetToken.IsUsed = true;

        // ================================================
        // 4. Rotate security stamp and revoke all active refresh tokens,
        //    so any device already logged in is signed out after a reset.
        // ================================================
        user.SecurityStamp = Guid.NewGuid().ToString("N");

        var activeTokens = await _context.RefreshTokens.Where(x => x.UserID == user.UserID && !x.IsRevoked).ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        _logger.LogInformation(
            "Rotated security stamp and revoked {Count} active session(s) for user {UserId} after password reset",
            activeTokens.Count,
            user.UserID);

        // ================================================
        // 5. Create audit log
        // ================================================
        _context.AuditLogs.Add(_auditService.Create(user.UserID, "Password Reset via Email Link"));

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

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
