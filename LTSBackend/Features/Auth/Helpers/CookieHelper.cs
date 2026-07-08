using LTSBackend.Services.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LTSBackend.Features.Auth.Helpers;

public sealed class CookieHelper
{
    private readonly JwtSettings _settings;
    private readonly ILogger<CookieHelper> _logger;

    public CookieHelper(IOptions<JwtSettings> options, ILogger<CookieHelper> logger)
    {
        _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }
    public void SetRefreshToken(HttpResponse response, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Attempted to set null or empty refresh token");
            return;
        }

        response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _settings.UseSecureCookies,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays)
            });

        _logger.LogDebug("Refresh token cookie set with {Days} days expiry", _settings.RefreshTokenDays);
    }
    public void RemoveRefreshToken(HttpResponse response)
    {
        response.Cookies.Delete(
            "refreshToken",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _settings.UseSecureCookies,
                SameSite = SameSiteMode.Strict
            });

        _logger.LogDebug("Refresh token cookie removed");
    }
}