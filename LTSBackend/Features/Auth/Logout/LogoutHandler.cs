using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Auth.Helpers;
using LTSBackend.Features.Auth.Logout;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.Logout;

public class LogoutHandler(AppDbContext _context, IHttpContextAccessor _httpContextAccessor, CookieHelper _cookieHelper, IAuditService _auditService, ILogger<LogoutHandler> _logger) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout attempt");

        // ================================================
        // 1. Read refresh token from cookie
        // ================================================
        var refreshToken = _httpContextAccessor.HttpContext?
            .Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Logout failed: No refresh token in cookie");
            throw new UnauthorizedException("Refresh token not found.");
        }

        // ================================================
        // 2. Find and load refresh token with user
        // ================================================
        var token = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Token == refreshToken,
                cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Logout failed: Refresh token not found in database");
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // ================================================
        // 3. Revoke refresh token
        // ================================================
        token.IsRevoked = true;
        _logger.LogInformation("Refresh token revoked for user: {UserId}", token.UserID);

        // ================================================
        // 4. Update login history
        // ================================================
        var loginHistory = await _context.LoginHistories
            .Where(x =>
                x.UserID == token.UserID &&
                !x.IsLoggedOut)
            .OrderByDescending(x => x.LoginTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (loginHistory != null)
        {
            loginHistory.IsLoggedOut = true;
            loginHistory.LogoutTime = DateTime.UtcNow;
            loginHistory.Status = "Logout";
            _logger.LogInformation("Login history updated for user: {UserId}", token.UserID);
        }

        // ================================================
        // 5. Create audit log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(token.UserID, "User Logout"));

        // ================================================
        // 6. Save all changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        // ================================================
        // 7. Delete refresh token cookie
        // ================================================
        _cookieHelper.RemoveRefreshToken(
            _httpContextAccessor.HttpContext!.Response);

        _logger.LogInformation("User {UserId} logged out successfully", token.UserID);

        return true;
    }
}