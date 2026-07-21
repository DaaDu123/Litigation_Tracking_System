using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Auth.Helpers;
using LTSBackend.Features.Auth.RefreshToken;
using LTSBackend.Services.Audit;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.RefreshToken;

public class RefreshTokenHandler(AppDbContext _context, IJwtService _jwtService, IHttpContextAccessor _httpContextAccessor, IAuditService _auditService, CookieHelper _cookieHelper, ILogger<RefreshTokenHandler> _logger) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDTO>
{
    public async Task<RefreshTokenResponseDTO> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh attempt");

        // ================================================
        // 1. Read refresh token from cookie
        // ================================================
        var refreshToken = _httpContextAccessor.HttpContext?
            .Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Token refresh failed: No refresh token in cookie");
            throw new UnauthorizedException("Refresh token not found.");
        }

        // ================================================
        // 2. Find stored refresh token with user and role
        // ================================================
        var storedToken = await _context.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Token == refreshToken,
                cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Token refresh failed: Refresh token not found in database");
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // ================================================
        // 3. Validate token state
        // ================================================
        if (storedToken.IsRevoked)
        {
            _logger.LogWarning("Token refresh failed: Token is revoked for user: {UserId}", storedToken.UserID);
            throw new UnauthorizedException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiryDate <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Token expired for user: {UserId}", storedToken.UserID);
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var user = storedToken.User;

        // ================================================
        // 4. Validate user
        // ================================================
        if (user == null)
        {
            _logger.LogError("Token refresh failed: User not found");
            throw new UnauthorizedException("User not found.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Token refresh failed: User account inactive: {UserId}", user.UserID);
            throw new UnauthorizedException("User account is inactive.");
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("Token refresh failed: User account deleted: {UserId}", user.UserID);
            throw new UnauthorizedException("User account has been deleted.");
        }

        // ================================================
        // 5. Revoke old refresh token (Token Rotation)
        // ================================================
        storedToken.IsRevoked = true;

        // ================================================
        // 6. Generate new access token
        // ================================================
        var accessToken = _jwtService.GenerateToken(user);
        var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();

        // ================================================
        // 7. Generate new refresh token
        // ================================================
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        _context.RefreshTokens.Add(new LTSBackend.Models.Security.RefreshToken
        {
            UserID = user.UserID,
            Token = newRefreshToken,
            ExpiryDate = newRefreshTokenExpiry,
            IsRevoked = false
        });

        // ================================================
        // 8. Create audit log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(user.UserID, "Token Refreshed"));

        // ================================================
        // 9. Save all changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        // ================================================
        // 10. Update refresh token cookie
        // ================================================
        _cookieHelper.SetRefreshToken(
            _httpContextAccessor.HttpContext!.Response,
            newRefreshToken);

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.UserID);

        return new RefreshTokenResponseDTO
        {
            AccessToken = accessToken,
            AccessTokenExpiry = accessTokenExpiry
        };
    }
}