using LTSBackend.Comman.Enum;

namespace LTSBackend.Services.Permissions;

public interface IPermissionService
{
    /// <summary>
    /// Check agar user ke paas specific permission hai
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);
    /// <summary>
    /// Get user ke tamam permissions
    /// </summary>
    Task<List<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check agar user specific role mein hai
    /// </summary>
    Task<bool> HasRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user ka role
    /// </summary>
    Task<UserRole?> GetUserRoleAsync(int userId, CancellationToken cancellationToken = default);
}