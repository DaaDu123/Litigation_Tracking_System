using LTSBackend.Models;
using System.Security.Cryptography;
using LTSBackend.Services.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LTSBackend.Services.Jwt
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Email, user.Email)
            };

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
    }
}
