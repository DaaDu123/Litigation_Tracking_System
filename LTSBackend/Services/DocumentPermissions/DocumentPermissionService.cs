using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.DocumentPermissions;

/// <summary>
/// Implementation of document permission service
/// Handles Moharrir blind upload (write-only) and elevated access modes
/// </summary>
public class DocumentPermissionService (AppDbContext _context, ILogger<DocumentPermissionService> _logger) : IDocumentPermissionService
{

    /// <summary>
    /// Check agar user ko specific document action allowed hai
    /// </summary>
    public async Task<bool> CanUserAccessDocumentAsync(int userId,long documentId,string action,CancellationToken cancellationToken = default)
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

            // ================================================
            // 2. Super Admin ko sab kuch access hai
            // ================================================
            if (user.GetRole() == UserRole.SuperAdmin)
            {
                _logger.LogDebug("Super Admin - full access to document {DocumentId} action {Action}", documentId, action);
                return true;
            }

            // ================================================
            // 3. Partner, Associate, InternParalegal ko full access
            // ================================================
            var role = user.GetRole();
            if (role == UserRole.Partner || role == UserRole.AssociateLawyer || role == UserRole.InternParalegal)
            {
                _logger.LogDebug("Role {Role} has full document access", role);
                return true;
            }

            // ================================================
            // 4. FirmAdmin ko full access
            // ================================================
            if (role == UserRole.FirmAdmin)
            {
                _logger.LogDebug("FirmAdmin has full document access");
                return true;
            }

            // ================================================
            // 5. Moharrir - special handling (blind upload feature)
            // ================================================
            if (role == UserRole.Moharrir)
            {
                return await HandleMohallirAccessAsync(
                    userId,
                    documentId,
                    action,
                    cancellationToken);
            }

            _logger.LogWarning("User {UserId} with role {Role} trying to access document {DocumentId} with action {Action}",
                userId, role, documentId, action);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error checking document access for user {UserId} document {DocumentId} action {Action}",
                userId, documentId, action);
            return false;
        }
    }

    /// <summary>
    /// Special handling for Moharrir access (restricted vs elevated)
    /// </summary>
    private async Task<bool> HandleMohallirAccessAsync(int userId,long documentId,string action,CancellationToken cancellationToken)
    {
        // ================================================
        // Check agar Moharrir ke paas elevated access hai
        // ================================================
        bool isElevated = await HasMohallirElevatedAccessAsync(userId, cancellationToken);

        if (isElevated)
        {
            // Elevated mode: view + download allowed
            if (action == "View" || action == "Download" || action == "Upload")
            {
                _logger.LogDebug("Moharrir {UserId} elevated mode - {Action} allowed", userId, action);
                return true;
            }
        }
        else
        {
            // Restricted mode: write-only (upload only)
            if (action == "Upload")
            {
                _logger.LogDebug("Moharrir {UserId} restricted mode - {Action} allowed", userId, action);
                return true;
            }
            else if (action == "View" || action == "Download")
            {
                _logger.LogDebug("Moharrir {UserId} restricted mode - {Action} DENIED", userId, action);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Check agar Moharrir ke paas elevated access hai
    /// Elevated mode = Can View + Download documents
    /// </summary>
    public async Task<bool> HasMohallirElevatedAccessAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null)
                return false;

            // Check agar user ke role ke paas ViewDocumentsIfPermitted permission hai
            var hasPermission = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserID == userId)
                .Include(x => x.Role!)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AnyAsync(x =>
                    x.Role!.RolePermissions.Any(rp =>
                        rp.Permission!.PermissionName == "ViewDocumentsIfPermitted"),
                    cancellationToken);

            _logger.LogDebug("Moharrir {UserId} elevated access check: {Result}", userId, hasPermission);
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Moharrir elevated access for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Check agar Moharrir restricted mode mein hai (write-only)
    /// </summary>
    public async Task<bool> IsMohallirRestrictedAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user?.GetRole() != UserRole.Moharrir)
                return false;

            // Agar elevated access nahi hai to restricted hai
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
    public async Task<DocumentAccessLevel> GetUserDocumentAccessLevelAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserID == userId, cancellationToken);

            if (user == null)
                return DocumentAccessLevel.None;

            var role = user.GetRole();

            // ================================================
            // Determine access level by role
            // ================================================
            return role switch
            {
                UserRole.SuperAdmin => DocumentAccessLevel.FullAccess,
                UserRole.FirmAdmin => DocumentAccessLevel.FullAccess,
                UserRole.Partner => DocumentAccessLevel.FullAccess,
                UserRole.AssociateLawyer => DocumentAccessLevel.ReadWrite,
                UserRole.InternParalegal => DocumentAccessLevel.ReadOnly,
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

    /// <summary>
    /// Get Moharrir's specific access level (restricted vs elevated)
    /// </summary>
    private async Task<DocumentAccessLevel> GetMohallirAccessLevelAsync(int userId,CancellationToken cancellationToken)
    {
        bool isElevated = await HasMohallirElevatedAccessAsync(userId, cancellationToken);
        return isElevated ? DocumentAccessLevel.ReadWrite : DocumentAccessLevel.WriteOnly;
    }

    /// <summary>
    /// Grant document permission to a role
    /// </summary>
    public async Task GrantDocumentPermissionAsync(
        long documentId,
        int roleId,
        bool canView,
        bool canDownload,
        bool canUpload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ================================================
            // Check agar permission pehle se exist karta hai
            // ================================================
            var existingPermission = await _context.DocumentPermissions
                .FirstOrDefaultAsync(x =>
                    x.DocumentID == documentId &&
                    x.RoleID == roleId,
                    cancellationToken);

            if (existingPermission != null)
            {
                // Update existing
                existingPermission.CanView = canView;
                existingPermission.CanDownload = canDownload;
                existingPermission.CanUpload = canUpload;
                _logger.LogInformation(
                    "Updated document permission for document {DocumentId} role {RoleId}",
                    documentId, roleId);
            }
            else
            {
                // Create new
                var permission = new Models.Cases.DocumentPermission
                {
                    DocumentID = documentId,
                    RoleID = roleId,
                    CanView = canView,
                    CanDownload = canDownload,
                    CanUpload = canUpload
                };

                _context.DocumentPermissions.Add(permission);
                _logger.LogInformation(
                    "Granted document permission for document {DocumentId} role {RoleId}",
                    documentId, roleId);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error granting document permission for document {DocumentId} role {RoleId}",
                documentId, roleId);
            throw;
        }
    }

    /// <summary>
    /// Revoke document permission
    /// </summary>
    public async Task RevokeDocumentPermissionAsync(long documentId,int roleId,CancellationToken cancellationToken = default)
    {
        try
        {
            var permission = await _context.DocumentPermissions
                .FirstOrDefaultAsync(x =>
                    x.DocumentID == documentId &&
                    x.RoleID == roleId,
                    cancellationToken);

            if (permission != null)
            {
                _context.DocumentPermissions.Remove(permission);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Revoked document permission for document {DocumentId} role {RoleId}",
                    documentId, roleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking document permission for document {DocumentId} role {RoleId}",
                documentId, roleId);
            throw;
        }
    }
}