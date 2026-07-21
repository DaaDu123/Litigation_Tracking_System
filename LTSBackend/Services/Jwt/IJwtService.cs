using LTSBackend.Models.Security;
namespace LTSBackend.Services.Jwt;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}