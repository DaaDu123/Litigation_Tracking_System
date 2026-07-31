using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LTSBackend.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler(AppDbContext _context, IEmailService _emailService, IConfiguration _configuration, ILogger<ForgotPasswordHandler> _logger) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseDTO>
{
    // Link is valid for 30 minutes - long enough for someone to check their
    // email without rushing, short enough to limit the exposure window of
    // an intercepted link.
    private const int TokenExpiryMinutes = 30;

    public async Task<ForgotPasswordResponseDTO> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Forgot password requested for email: {Email}", request.Email);

        // ================================================
        // Generic response - we always return this regardless of whether
        // the email exists or not. This protects against "user enumeration"
        // attacks (so no one can guess which email is registered).
        // ================================================
        var genericResponse = new ForgotPasswordResponseDTO
        {
            Email = request.Email,
            Message = "If this email is registered in our system, a password reset link has been sent. Please also check your spam/junk folder."
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
            _logger.LogWarning("Forgot password: No user found for {Email} (returning a generic response)", request.Email);
            return genericResponse;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Forgot password: User is inactive: {Email} (returning generic response)", request.Email);
            return genericResponse;
        }

        // ================================================
        // 2. Invalidate any old, unused reset tokens for this user so only
        //    the most recently requested link is ever valid.
        // ================================================
        var oldTokens = await _context.PasswordResetTokens
            .Where(x => x.Email == request.Email && !x.IsUsed)
            .ToListAsync(cancellationToken);

        if (oldTokens.Count > 0)
        {
            _context.PasswordResetTokens.RemoveRange(oldTokens);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} old password-reset token(s) for {Email}", oldTokens.Count, request.Email);
        }

        // ================================================
        // 3. Generate a cryptographically secure token. Only its SHA-256
        //    hash is stored - the raw value exists solely in the email
        //    link, so a database leak cannot be used to reset passwords.
        // ================================================
        string rawToken = GenerateSecureToken();
        string tokenHash = HashToken(rawToken);

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Email = request.Email,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes),
            IsUsed = false,
            UserID = user.UserID,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password-reset token generated for user: {UserId}", user.UserID);

        // ================================================
        // 4. Build the reset link and email it - fail silently (still
        //    return the generic response) so an attacker can't use send
        //    failures to enumerate valid accounts.
        // ================================================
        try
        {
            string frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            string resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

            await _emailService.SendPasswordResetLinkAsync(user.Email, user.FullName, resetLink, TokenExpiryMinutes);
            _logger.LogInformation("Password-reset link email sent to: {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset link email to: {Email}", request.Email);
            // Don't throw - for security we keep returning the generic response
        }

        return genericResponse;
    }

    private static string GenerateSecureToken()
    {
        // 32 random bytes -> URL-safe base64 (no padding), i.e. a 43-char
        // single-use token with 256 bits of entropy.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
