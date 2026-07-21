using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LTSBackend.Models.Security;
using Microsoft.IdentityModel.Tokens;

namespace LTSBackend.Services.Jwt;

public class JwtService(IConfiguration _configuration) : IJwtService
{
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        // Add role claim
        var role = user.GetRole();
        if (role.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }
        else if (!string.IsNullOrWhiteSpace(user.Role?.RoleName))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));
        }

        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
        var issuer = _configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException("JwtSettings:Issuer is not configured.");
        var audience = _configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException("JwtSettings:Audience is not configured.");

        if (!int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var expiryMinutes))
            expiryMinutes = 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetAccessTokenExpiry()
    {
        int expiryMinutes = _configuration.GetValue<int>("JwtSettings:ExpiryMinutes", 60);
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }
    public DateTime GetRefreshTokenExpiry()
    {
        int refreshTokenDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenDays", 7);
        return DateTime.UtcNow.AddDays(refreshTokenDays);
    }
}