using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.Permissions;

public class PermissionService(AppDbContext _context, ILogger<PermissionService> _logger) : IPermissionService
{
    /// <summary>
    /// Checks whether the user has a specific permission
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
                _logger.LogWarning("User not found or has no role: {UserId}", userId);
                return false;
            }

            // ================================================
            // 2. Super Admin has all permissions
            // ================================================
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                _logger.LogDebug("Super Admin - all permissions granted");
                return true;
            }

            // ================================================
            // 3. Check the role's permissions
            // ================================================
            bool hasPermission = user.Role.RolePermissions.Any(rp => rp.Permission.PermissionName == permission);

            _logger.LogDebug("Permission check for user {UserId} on {Permission}: {Result}",userId, permission, hasPermission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission: {Permission} for user {UserId}",permission, userId);
            return false;
        }
    }

    /// <summary>
    /// Gets all permissions for the user
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

            // Super Admin - all permissions
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                return Enum.GetNames(typeof(PermissionEnum)).ToList();
            }

            // Role's permissions
            var permissions = user.Role.RolePermissions
                .Select(x => x.Permission.PermissionName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            _logger.LogInformation("User {UserId} has {Count} permissions", userId, permissions.Count);

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permissions: {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks whether the user is in a specific role
    /// </summary>
    public async Task<bool> HasRoleAsync(int userId,UserRole role,CancellationToken cancellationToken = default)
    {
        try
        {
            // FIX: Fetch RoleID from the DB first (primitive type - EF can translate this)
            // then do the enum conversion in memory (GetRole() cannot be translated to SQL)
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
            _logger.LogError(ex, "Error checking role: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Gets the user's role
    /// </summary>
    public async Task<UserRole?> GetUserRoleAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            // FIX: Fetch RoleID from the DB first, then do the enum conversion in memory
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
            _logger.LogError(ex, "Error fetching user role: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Full firm case-directory visibility (SuperAdmin / FirmAdmin / Partner only —
    /// see the "View Firm Case Directory" row of the Roles &amp; Permissions Matrix).
    /// Everyone else (AssociateLawyer, Moharrir, InternParalegal) must be scoped to
    /// their own case assignments; see IsUserAssignedToCaseAsync.
    /// </summary>
    public async Task<bool> HasFullCaseDirectoryVisibilityAsync(int userId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleAsync(userId, cancellationToken);
        return role == UserRole.SuperAdmin || role == UserRole.FirmAdmin || role == UserRole.Partner;
    }

    /// <summary>
    /// Active-assignment check for BOLA protection on case-level endpoints.
    /// An assignment is considered active when EndDate is null or still in the future
    /// (mirrors the convention already used by DocumentPermissionService for documents).
    /// </summary>
    public async Task<bool> IsUserAssignedToCaseAsync(int userId, long caseId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.CaseAssignments
            .AsNoTracking()
            .AnyAsync(a => a.CaseID == caseId && a.UserID == userId && (a.EndDate == null || a.EndDate > now), cancellationToken);
    }
}