using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.Permissions;

public class PermissionService(AppDbContext _context, ILogger<PermissionService> _logger) : IPermissionService
{
    // Checks whether a user holds a specific permission. Denies (returns false)
    // for missing/roleless/inactive/deleted/blocked-firm users, so a permission
    // check can never silently "pass" for an account that should not be able
    // to act at all - this is enforced here in addition to the JWT-level
    // active-status check (Program.cs OnTokenValidated) as defence in depth.
    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default)
    {
        try
        {
            // ================================================
            // 1. Load the user with role + role-permissions + firm status.
            //    IgnoreQueryFilters(): this service is also used by the
            //    authorization handler itself, so it must be able to see
            //    the record regardless of the caller's own tenant scope in
            //    order to correctly evaluate it (and then explicitly reject
            //    inactive/blocked accounts below, rather than have them
            //    silently disappear behind a filter).
            // ================================================
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(x => x.Role)
                .ThenInclude(x => x!.RolePermissions)
                .ThenInclude(x => x.Permission)
                .Include(x => x.Firm)
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.Role == null)
            {
                _logger.LogWarning("Permission check denied - user not found or has no role: {UserId}", userId);
                return false;
            }

            // ================================================
            // 2. Reject deactivated, soft-deleted, or blocked-firm accounts.
            //    A valid, unexpired JWT does not by itself mean the account
            //    is still allowed to act - Firm Admins can deactivate/block
            //    users at any time, and Super Admin can suspend a firm.
            // ================================================
            if (user.IsDeleted || !user.IsActive)
            {
                _logger.LogWarning("Permission check denied - account inactive or deleted: {UserId}", userId);
                return false;
            }

            if (user.Firm != null && (user.Firm.IsBlocked || user.Firm.IsDeleted))
            {
                _logger.LogWarning("Permission check denied - firm is blocked/deleted for user {UserId}", userId);
                return false;
            }

            // ================================================
            // 3. Super Admin implicitly holds every permission.
            // ================================================
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                _logger.LogDebug("Super Admin - all permissions granted for user {UserId}", userId);
                return true;
            }

            // ================================================
            // 4. Otherwise check the role's granted permissions.
            // ================================================
            bool hasPermission = user.Role.RolePermissions.Any(rp => rp.Permission.PermissionName == permission);

            _logger.LogDebug("Permission check for user {UserId} on {Permission}: {Result}", userId, permission, hasPermission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    // Returns the full list of permission names available to the user (every
    // permission for SuperAdmin, otherwise the role's granted permission set).
    public async Task<List<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(x => x.Role)
                .ThenInclude(x => x!.RolePermissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.Role == null || user.IsDeleted || !user.IsActive)
            {
                return [];
            }

            // Super Admin - every permission that exists.
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                return Enum.GetNames(typeof(PermissionEnum)).ToList();
            }

            // Otherwise, the role's granted permissions.
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
            _logger.LogError(ex, "Error while fetching permissions for user {UserId}", userId);
            return [];
        }
    }

    // Checks whether the user's currently assigned role matches the given role.
    public async Task<bool> HasRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch RoleID (a plain int, translatable to SQL) first, then convert
            // to the UserRole enum in memory - GetRole() itself cannot be
            // translated by EF Core's SQL provider.
            var roleId = await _context.Users
                .IgnoreQueryFilters()
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
            _logger.LogError(ex, "Error while checking role for user {UserId}", userId);
            return false;
        }
    }

    // Returns the user's current role as an enum, or null if unset/unrecognised.
    public async Task<UserRole?> GetUserRoleAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Same pattern as HasRoleAsync: fetch the primitive RoleID via SQL,
            // then convert to the enum in memory.
            var roleId = await _context.Users
                .IgnoreQueryFilters()
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
            _logger.LogError(ex, "Error while fetching role for user {UserId}", userId);
            return null;
        }
    }

    // SRS "View Firm Case Directory": SuperAdmin always sees everything;
    // otherwise this is a plain permission check against ViewFirmCaseDirectory
    // (granted to FirmAdmin and Partner in SeedRolePermissions).
    public async Task<bool> HasFullCaseDirectoryVisibilityAsync(int userId, CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleAsync(userId, cancellationToken);
        if (role == UserRole.SuperAdmin)
            return true;

        return await HasPermissionAsync(userId, nameof(PermissionEnum.ViewFirmCaseDirectory), cancellationToken);
    }

    // SRS RBAC "Case assignment" check: confirms both that the case belongs
    // to the user's own firm (tenant isolation) AND that an active
    // CaseAssignment row links this user to it. Deliberately does NOT treat
    // SuperAdmin/full-directory-visibility roles as "assigned" - callers
    // that want those roles to bypass the assignment check should call
    // HasFullCaseDirectoryVisibilityAsync first.
    public async Task<bool> IsUserAssignedToCaseAsync(int userId, long caseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.IsDeleted || !user.IsActive)
                return false;

            // Tenant isolation: the case must belong to the same firm as the
            // user before assignment is even considered.
            var caseFirmId = await _context.Cases
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.CaseID == caseId)
                .Select(c => (int?)c.FirmID)
                .FirstOrDefaultAsync(cancellationToken);

            if (caseFirmId == null || caseFirmId != user.FirmID)
            {
                _logger.LogWarning("User {UserId} (Firm {UserFirmId}) denied cross-firm case-assignment check on case {CaseId} (Firm {CaseFirmId})",
                    userId, user.FirmID, caseId, caseFirmId);
                return false;
            }

            var now = DateTime.UtcNow;
            bool isAssigned = await _context.CaseAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(a => a.CaseID == caseId && a.UserID == userId && (a.EndDate == null || a.EndDate > now), cancellationToken);

            return isAssigned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking case assignment for user {UserId} case {CaseId}", userId, caseId);
            return false;
        }
    }
}