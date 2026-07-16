using LTSBackend.Comman.Enum;

namespace LTSBackend.Services.DocumentPermissions;

/// <summary>
/// Manages document access permissions, including Moharrir blind upload feature
/// </summary>
public interface IDocumentPermissionService
{
    /// <summary>
    /// Check agar user ko document access hai (View/Download/Upload)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="action">Action: "View", "Download", "Upload"</param>
    Task<bool> CanUserAccessDocumentAsync(int userId, long documentId, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check agar Moharrir ke paas elevated access hai (view/download allowed)
    /// </summary>
    Task<bool> HasMohallirElevatedAccessAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check agar Moharrir restricted mode mein hai (blind upload only)
    /// </summary>
    Task<bool> IsMohallirRestrictedAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user ke document access level
    /// </summary>
    Task<DocumentAccessLevel> GetUserDocumentAccessLevelAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant document permission to role
    /// </summary>
    Task GrantDocumentPermissionAsync(long documentId, int roleId, bool canView, bool canDownload, bool canUpload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant document permission to a specific user (overrides role-level grant)
    /// </summary>
    Task GrantUserDocumentPermissionAsync(long documentId, int userId, bool canView, bool canDownload, bool canUpload, CancellationToken cancellationToken = default);   // ✅ new

    /// <summary>
    /// Revoke document permission
    /// </summary>
    Task RevokeDocumentPermissionAsync(long documentId, int roleId, CancellationToken cancellationToken = default);
}