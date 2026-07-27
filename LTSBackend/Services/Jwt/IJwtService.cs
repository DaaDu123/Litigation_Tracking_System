using LTSBackend.Models.Security;
namespace LTSBackend.Services.Jwt;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();

    /// <summary>
    /// Hashes a raw refresh token (SHA-256, hex-encoded) for storage/lookup.
    /// SECURITY: refresh tokens must never be stored in plaintext — if the
    /// database is ever read (breach, backup leak, insider access), a
    /// plaintext token is immediately usable to impersonate the user for
    /// its full lifetime. Only the hash is persisted; the raw value only
    /// ever exists in the HttpOnly cookie sent to the client.
    /// </summary>
    string HashRefreshToken(string rawToken);

    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}