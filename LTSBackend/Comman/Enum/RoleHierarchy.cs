namespace LTSBackend.Comman.Enum;

public static class RoleHierarchy
{
    // Lower number = higher privilege (UserRole enum values already follow this)
    public static bool CanAssignRole(UserRole actingUserRole, int targetRoleId)
    {
        if (!System.Enum.IsDefined(typeof(UserRole), targetRoleId))
            return false;

        var targetRole = (UserRole)targetRoleId;

        // SuperAdmin is platform-only — nobody should be able to assign it
        // via the normal user-management endpoints.
        if (targetRole == UserRole.SuperAdmin)
            return false;

        // A user can only assign a role that is STRICTLY lower privilege
        // than their own (numerically greater), never equal or higher.
        // This is what enforces the SRS rule "Firm Admin cannot create
        // another Firm Admin" - the only caller of this method today is
        // FirmAdmin (Create/UpdateUser are both [Authorize(Roles =
        // RoleNames.FirmAdminAndAbove)], i.e. FirmAdmin only), and with a
        // strict ">" a FirmAdmin(2) can no longer assign FirmAdmin(2) to
        // someone else - only Partner/AssociateLawyer/Moharrir/InternParalegal
        // (3,4,5,6), exactly as the SRS's "Firm Admin can create only:
        // Partner, Associate Lawyer, Moharrir, Intern/Paralegal" states.
        return (int)targetRole > (int)actingUserRole;
    }
}