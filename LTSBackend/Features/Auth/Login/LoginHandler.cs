using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Auth.Helpers;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using CookieHelper = LTSBackend.Features.Auth.Helpers.CookieHelper;

namespace LTSBackend.Features.Auth.Login;
public class LoginHandler (AppDbContext _context, IPasswordService _passwordService, IJwtService _jwtService, IAuditService _auditService, IHttpContextAccessor _httpContextAccessor, CookieHelper _cookieHelper, ILogger<LoginHandler> _logger) : IRequestHandler<LoginCommand, LoginResponseDTO>
{
    public async Task<LoginResponseDTO> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // ================================================
        // 1. Find user by email with role
        // ================================================
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Email == request.Email,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email: {Email}", request.Email);
            throw new UnauthorizedException("Invalid credentials.");
        }

        // ================================================
        // 2. Verify password
        // ================================================
        bool passwordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for user: {UserId}", user.UserID);
            throw new UnauthorizedException("Invalid credentials.");
        }

        // ================================================
        // 3. Check if account is deleted
        // ================================================
        if (user.IsDeleted)
        {
            _logger.LogWarning("Login failed: Account is deleted for user: {UserId}", user.UserID);
            throw new UnauthorizedException("Account has been deleted.");
        }

        // ================================================
        // 4. Check if account is active
        // ================================================
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account is inactive for user: {UserId}", user.UserID);
            throw new ValidationException(
                new List<string> { "Please verify your email address before logging in." });
        }

        // ================================================
        // 5. Update last login time
        // ================================================
        user.LastLogin = DateTime.UtcNow;

        // ================================================
        // 6. Generate access token
        // ================================================
        var accessToken = _jwtService.GenerateToken(user);
        var accessTokenExpiry = _jwtService.GetAccessTokenExpiry();

        // ================================================
        // 7. Generate refresh token
        // ================================================
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        // ================================================
        // 8. Save refresh token to database
        // ================================================
        _context.RefreshTokens.Add(new LTSBackend.Models.Security.RefreshToken
        {
            UserID = user.UserID,
            Token = refreshToken,
            ExpiryDate = refreshTokenExpiry,
            IsRevoked = false
        });

        // ================================================
        // 9. Record login in LoginHistory
        // ================================================
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

        _context.LoginHistories.Add(new LTSBackend.Models.Security.LoginHistory
        {
            UserID = user.UserID,
            LoginTime = DateTime.UtcNow,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            Status = "Success",
            IsLoggedOut = false
        });

        // ================================================
        // 10. Create audit log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(user.UserID, "User Login"));

        // ================================================
        // 11. Save all changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        // ================================================
        // 12. Set refresh token in HTTP cookie
        // ================================================
        _cookieHelper.SetRefreshToken(
            _httpContextAccessor.HttpContext!.Response,
            refreshToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.UserID);

        return new LoginResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = accessToken,
            AccessTokenExpiry = accessTokenExpiry
        };
    }
}