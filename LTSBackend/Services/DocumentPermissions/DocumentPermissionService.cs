using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.DocumentPermissions;

/// <summary>
/// Implementation of document permission service
/// Handles Moharrir blind upload (write-only) and elevated access modes
/// </summary>
public class DocumentPermissionService(AppDbContext _context, ILogger<DocumentPermissionService> _logger) : IDocumentPermissionService
{
    public async Task<bool> CanUserAccessDocumentAsync(int userId, long documentId, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            // ================================================
            // 1. Get user with role
            // ================================================
            var user = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null || user.Role == null)
            {
                _logger.LogWarning("User not found or has no role: {UserId}", userId);
                return false;
            }

            var role = user.GetRole();
            // ================================================
            // 2. Super Admin ko sab kuch access hai (system-wide, no firm scope)
            // ================================================
            if (role == UserRole.SuperAdmin)
            {
                _logger.LogDebug("Super Admin - full access to document {DocumentId} action {Action}", documentId, action);
                return true;
            }

            // ================================================
            // FIX (CRITICAL): Multi-tenant isolation.
            // A user must NEVER access a document belonging to another
            // firm's case, no matter what role they have. Previously
            // FirmAdmin/Partner returned true unconditionally below,
            // which let anyone with that role read/download/delete ANY
            // firm's documents just by guessing a DocumentID.
            // ================================================
            if (documentId > 0)
            {
                var documentFirmId = await _context.Documents
                    .AsNoTracking()
                    .Where(d => d.DocumentID == documentId)
                    .Select(d => (int?)d.Case.FirmID)
                    .FirstOrDefaultAsync(cancellationToken);

                if (documentFirmId == null || documentFirmId != user.FirmID)
                {
                    _logger.LogWarning(
                        "User {UserId} (Firm {UserFirmId}) denied cross-firm access to document {DocumentId} (Firm {DocFirmId})",
                        userId, user.FirmID, documentId, documentFirmId);
                    return false;
                }
            }

            // ================================================
            // 3. FirmAdmin and Partner have full access (View/Download/Upload/Delete)
            //    within their own firm (enforced above)
            // ================================================
            if (role == UserRole.FirmAdmin || role == UserRole.Partner)
            {
                _logger.LogDebug("Role {Role} has full document access (own firm only)", role);
                return true;
            }

            // ================================================
            // 4. AssociateLawyer -> ReadWrite (View + Download + Upload)
            // ================================================
            if (role == UserRole.AssociateLawyer)
            {
                bool actionAllowed = action == "View" || action == "Download" || action == "Upload";
                if (!actionAllowed) return false;

                bool isAssigned = await IsUserAssignedToDocumentCaseAsync(userId, documentId, cancellationToken);
                _logger.LogDebug("AssociateLawyer {UserId} - {Action} on doc {DocumentId}, assigned={Assigned}", userId, action, documentId, isAssigned);
                return isAssigned;
            }

            if (role == UserRole.InternParalegal)
            {
                bool actionAllowed = action == "View" || action == "Download";
                if (!actionAllowed) return false;
                return await IsUserAssignedToDocumentCaseAsync(userId, documentId, cancellationToken);
            }

            if (role == UserRole.Moharrir)
            {
                return await HandleMohallirAccessAsync(userId, documentId, action, cancellationToken);
            }

            _logger.LogWarning("User {UserId} with role {Role} trying to access document {DocumentId} with action {Action}", userId, role, documentId, action);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking document access for user {UserId} document {DocumentId} action {Action}", userId, documentId, action);
            return false;
        }
    }

    /// <summary>
    /// Case active assignment check. CaseAssignments table has no IsActive
    /// column — an assignment is treated as active when EndDate is null
    /// (or in the future).
    /// </summary>
    private async Task<bool> IsUserAssignedToDocumentCaseAsync(int userId, long documentId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await _context.Documents.AsNoTracking().Where(d => d.DocumentID == documentId)
            .Join(_context.CaseAssignments.AsNoTracking().Where(a => a.UserID == userId && (a.EndDate == null || a.EndDate > now)),
                d => d.CaseID,
                a => a.CaseID,
                (d, a) => a)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Special handling for Moharrir access (restricted vs elevated).
    /// Checks per-document permission grants in priority order:
    /// 1. User-specific grant (DocumentPermissions.UserID) — highest priority,
    ///    lets an admin elevate/restrict one specific Moharrir individually.
    /// 2. Role-based grant (DocumentPermissions.RoleID) — applies to all
    ///    users sharing that role.
    /// 3. Fallback — role-level elevated/restricted default via
    ///    "ViewDocumentsIfPermitted" permission.
    /// </summary>
    private async Task<bool> HandleMohallirAccessAsync(int userId, long documentId, string action, CancellationToken cancellationToken)
    {
        if (action == "Upload")
        {
            _logger.LogDebug("Moharrir {UserId} - Upload allowed by default", userId);
            return true;
        }

        if (documentId > 0)
        {
            var isAssigned = await IsUserAssignedToDocumentCaseAsync(userId, documentId, cancellationToken);
            if (!isAssigned)
            {
                _logger.LogDebug("Moharrir {UserId} not assigned to case for document {DocumentId} — denied", userId, documentId);
                return false;
            }

            // ================================================
            // 1. Pehle user-specific grant check karo (highest priority)
            // ================================================
            var userPermission = await _context.DocumentPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DocumentID == documentId && x.UserID == userId, cancellationToken);

            if (userPermission != null)
            {
                bool userAllowed = action == "View" ? userPermission.CanView
                    : action == "Download" && userPermission.CanDownload;

                _logger.LogDebug("Moharrir {UserId} - user-specific permission found for document {DocumentId}, {Action} allowed: {Allowed}", userId, documentId, action, userAllowed);
                return userAllowed;
            }

            // ================================================
            // 2. Warna role-based grant check karo
            // ================================================
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

            if (user?.RoleID != null)
            {
                var rolePermission = await _context.DocumentPermissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DocumentID == documentId && x.RoleID == user.RoleID, cancellationToken);

                if (rolePermission != null)
                {
                    bool roleAllowed = action == "View" ? rolePermission.CanView
                        : action == "Download" && rolePermission.CanDownload;
                    _logger.LogDebug("Moharrir {UserId} - role-based permission found for document {DocumentId}, {Action} allowed: {Allowed}", userId, documentId, action, roleAllowed);
                    return roleAllowed;
                }
            }
        }

        // ================================================
        // 3. Fallback: no explicit per-document row -> role-level default
        // (restricted = write-only, elevated = view+download+upload)
        // ================================================
        bool isElevated = await HasMohallirElevatedAccessAsync(userId, cancellationToken);
        if (isElevated && (action == "View" || action == "Download"))
        {
            _logger.LogDebug("Moharrir {UserId} elevated mode (default) - {Action} allowed", userId, action);
            return true;
        }

        _logger.LogDebug("Moharrir {UserId} restricted mode (default) - {Action} DENIED", userId, action);
        return false;
    }

    public async Task<bool> HasMohallirElevatedAccessAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasPermission = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserID == userId)
                .Include(x => x.Role!)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AnyAsync(x => x.Role != null && x.Role.RolePermissions.Any(rp => rp.Permission!.PermissionName == "ViewDocumentsIfPermitted"), cancellationToken);

            _logger.LogDebug("Moharrir {UserId} elevated access check: {Result}", userId, hasPermission);
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Moharrir elevated access for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsMohallirRestrictedAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user?.GetRole() != UserRole.Moharrir)
                return false;

            bool isRestricted = !await HasMohallirElevatedAccessAsync(userId, cancellationToken);

            _logger.LogDebug("Moharrir {UserId} restricted check: {IsRestricted}", userId, isRestricted);
            return isRestricted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Moharrir restricted mode for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get user's document access level
    /// </summary>
    public async Task<DocumentAccessLevel> GetUserDocumentAccessLevelAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null)
                return DocumentAccessLevel.None;

            var role = user.GetRole();

            return role switch
            {
                UserRole.SuperAdmin => DocumentAccessLevel.FullAccess,
                UserRole.FirmAdmin => DocumentAccessLevel.FullAccess,
                UserRole.Partner => DocumentAccessLevel.FullAccess,
                UserRole.AssociateLawyer => DocumentAccessLevel.ReadWrite,
                UserRole.InternParalegal => DocumentAccessLevel.ReadWrite,
                UserRole.Moharrir => await GetMohallirAccessLevelAsync(userId, cancellationToken),
                _ => DocumentAccessLevel.None
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document access level for user {UserId}", userId);
            return DocumentAccessLevel.None;
        }
    }

    private async Task<DocumentAccessLevel> GetMohallirAccessLevelAsync(int userId, CancellationToken cancellationToken)
    {
        bool isElevated = await HasMohallirElevatedAccessAsync(userId, cancellationToken);
        return isElevated ? DocumentAccessLevel.ReadWrite : DocumentAccessLevel.WriteOnly;
    }

    /// <summary>
    /// Grant document permission to a role
    /// </summary>
    public async Task GrantDocumentPermissionAsync(long documentId, int roleId, bool canView, bool canDownload, bool canUpload, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingPermission = await _context.DocumentPermissions.FirstOrDefaultAsync(x => x.DocumentID == documentId && x.RoleID == roleId, cancellationToken);

            if (existingPermission != null)
            {
                existingPermission.CanView = canView;
                existingPermission.CanDownload = canDownload;
                existingPermission.CanUpload = canUpload;
                _logger.LogInformation("Updated document permission for document {DocumentId} role {RoleId}", documentId, roleId);
            }
            else
            {
                var permission = new Models.Cases.DocumentPermission
                {
                    DocumentID = documentId,
                    RoleID = roleId,
                    CanView = canView,
                    CanDownload = canDownload,
                    CanUpload = canUpload,
                    GrantedDate = DateTime.UtcNow
                };

                _context.DocumentPermissions.Add(permission);
                _logger.LogInformation("Granted document permission for document {DocumentId} role {RoleId}", documentId, roleId);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting document permission for document {DocumentId} role {RoleId}", documentId, roleId);
            throw;
        }
    }

    /// <summary>
    /// Grant document permission to a specific user (overrides role-level grant).
    /// </summary>
    public async Task GrantUserDocumentPermissionAsync(long documentId, int userId, bool canView, bool canDownload, bool canUpload, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingPermission = await _context.DocumentPermissions
                .FirstOrDefaultAsync(x => x.DocumentID == documentId && x.UserID == userId, cancellationToken);

            if (existingPermission != null)
            {
                existingPermission.CanView = canView;
                existingPermission.CanDownload = canDownload;
                existingPermission.CanUpload = canUpload;
                _logger.LogInformation("Updated user-specific document permission for document {DocumentId} user {UserId}", documentId, userId);
            }
            else
            {
                var permission = new Models.Cases.DocumentPermission
                {
                    DocumentID = documentId,
                    UserID = userId,
                    RoleID = null,
                    CanView = canView,
                    CanDownload = canDownload,
                    CanUpload = canUpload,
                    GrantedDate = DateTime.UtcNow
                };

                _context.DocumentPermissions.Add(permission);
                _logger.LogInformation("Granted user-specific document permission for document {DocumentId} user {UserId}", documentId, userId);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting user-specific document permission for document {DocumentId} user {UserId}", documentId, userId);
            throw;
        }
    }

    /// <summary>
    /// Revoke document permission (role-based)
    /// </summary>
    public async Task RevokeDocumentPermissionAsync(long documentId, int roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var permission = await _context.DocumentPermissions
                .FirstOrDefaultAsync(x => x.DocumentID == documentId && x.RoleID == roleId, cancellationToken);

            if (permission != null)
            {
                _context.DocumentPermissions.Remove(permission);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Revoked document permission for document {DocumentId} role {RoleId}", documentId, roleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking document permission for document {DocumentId} role {RoleId}", documentId, roleId);
            throw;
        }
    }
}