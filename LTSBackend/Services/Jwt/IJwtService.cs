using LTSBackend.Models.Security;
namespace LTSBackend.Services.Jwt;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();

    string HashRefreshToken(string rawToken);

    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}