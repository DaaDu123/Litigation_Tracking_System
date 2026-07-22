using LTSBackend.Models.Security;
using System.Threading.Tasks;

namespace LTSBackend.Services.Jwt
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateRefreshTokenAsync(int userId);
        Task<bool> ValidateRefreshTokenAsync(int userId, string token);
        Task RevokeRefreshTokenAsync(int userId);
        Task CleanupExpiredTokensAsync();
    }
}
 