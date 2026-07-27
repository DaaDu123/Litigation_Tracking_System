using LTSBackend.Comman.Enum;

namespace LTSBackend.Services.Permissions;

public interface IPermissionService
{
    /// <summary>
    /// Checks whether the user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets all permissions for the user
    /// </summary>
    Task<List<string>> GetPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user is in a specific role
    /// </summary>
    Task<bool> HasRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's role
    /// </summary>
    Task<UserRole?> GetUserRoleAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user has full, unfiltered visibility over every case in
    /// their firm (SuperAdmin / FirmAdmin / Partner per the Roles &amp; Permissions
    /// Matrix "View Firm Case Directory" row). Returns false for AssociateLawyer,
    /// Moharrir, and InternParalegal, who must only see cases they are assigned to.
    /// </summary>
    Task<bool> HasFullCaseDirectoryVisibilityAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user has an active assignment (EndDate is null or in the
    /// future) on the given case. Used to enforce case-level Broken Object Level
    /// Authorization (BOLA) protection for roles without full directory visibility.
    /// </summary>
    Task<bool> IsUserAssignedToCaseAsync(int userId, long caseId, CancellationToken cancellationToken = default);
}