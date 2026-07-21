using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.Permissions;

public class PermissionService(AppDbContext _context, ILogger<PermissionService> _logger) : IPermissionService
{
    /// <summary>
    /// Check agar user ke paas specific permission hai
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId,string permission,CancellationToken cancellationToken = default)
    {
        try
        {
            // ================================================
            // 1. Get user with role
            // ================================================
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.Role == null)
            {
                _logger.LogWarning("User nahi mila ya role nahi hai: {UserId}", userId);
                return false;
            }

            // ================================================
            // 2. Super Admin ko sab permissions hai
            // ================================================
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                _logger.LogDebug("Super Admin - tamam permissions grant");
                return true;
            }

            // ================================================
            // 3. Check role ke permissions
            // ================================================
            bool hasPermission = user.Role.RolePermissions.Any(rp => rp.Permission.PermissionName == permission);

            _logger.LogDebug("Permission check for user {UserId} on {Permission}: {Result}",userId, permission, hasPermission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permission check mein error: {Permission} for user {UserId}",permission, userId);
            return false;
        }
    }

    /// <summary>
    /// Get user ke tamam permissions
    /// </summary>
    public async Task<List<string>> GetPermissionsAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.Role == null)
            {
                return new List<string>();
            }

            // Super Admin - sab permissions
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                return Enum.GetNames(typeof(Permission)).ToList();
            }

            // Role ke permissions
            var permissions = user.Role.RolePermissions
                .Select(x => x.Permission.PermissionName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            _logger.LogInformation("User {UserId} ke {Count} permissions", userId, permissions.Count);

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permissions fetch mein error: {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Check agar user specific role mein hai
    /// </summary>
    public async Task<bool> HasRoleAsync(int userId,UserRole role,CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ FIX: RoleID pehle DB se nikalo (primitive type - EF translate kar sakta hai)
            // phir memory me enum conversion karo (GetRole() SQL translate nahi ho sakta)
            var roleId = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserID == userId)
                .Select(x => x.RoleID)
                .FirstOrDefaultAsync(cancellationToken);

            if (!roleId.HasValue || !Enum.IsDefined(typeof(UserRole), roleId.Value))
            {
                return false;
            }

            var userRole = (UserRole)roleId.Value;
            return userRole == role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role check mein error: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get user ka role
    /// </summary>
    public async Task<UserRole?> GetUserRoleAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ FIX: RoleID pehle DB se nikalo, phir memory me enum conversion karo
            var roleId = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserID == userId)
                .Select(x => x.RoleID)
                .FirstOrDefaultAsync(cancellationToken);

            if (!roleId.HasValue || !Enum.IsDefined(typeof(UserRole), roleId.Value))
            {
                return null;
            }

            return (UserRole)roleId.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User role fetch mein error: {UserId}", userId);
            return null;
        }
    }
}