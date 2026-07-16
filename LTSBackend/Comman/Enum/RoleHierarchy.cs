namespace LTSBackend.Comman.Enum;
public static class RoleHierarchy
{
    // Lower number = higher privilege (UserRole enum values already follow this)
    public static bool CanAssignRole(UserRole actingUserRole, int targetRoleId)
    {
        if (!System.Enum.IsDefined(typeof(UserRole), targetRoleId))   // ✅ fixed
            return false;

        var targetRole = (UserRole)targetRoleId;

        // SuperAdmin is platform-only — nobody should be able to assign it
        // via the normal user-management endpoints.
        if (targetRole == UserRole.SuperAdmin)
            return false;

        // A user can only assign a role that is equal-or-lower privilege
        // than their own (numerically >=), and never assign a higher role.
        return (int)targetRole >= (int)actingUserRole;
    }
}