using LTSBackend.Models;
namespace LTSBackend.Services.Jwt;
public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}