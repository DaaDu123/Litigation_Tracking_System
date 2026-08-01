namespace LTSFrontend.Core.Enums
{
    /// <summary>Mirrors LTSBackend.Comman.Enum.UserRole - keep IDs in sync.</summary>
    public enum UserRole
    {
        SuperAdmin = 1,
        FirmAdmin = 2,
        Partner = 3,
        AssociateLawyer = 4,
        Moharrir = 5,
        InternParalegal = 6
    }

    public static class UserRoleExtensions
    {
        public static IReadOnlyList<(int Id, string Name)> All { get; } = new List<(int, string)>
        {
            ((int)UserRole.SuperAdmin, "Super Admin"),
            ((int)UserRole.FirmAdmin, "Firm Admin"),
            ((int)UserRole.Partner, "Partner"),
            ((int)UserRole.AssociateLawyer, "Associate Lawyer"),
            ((int)UserRole.Moharrir, "Moharrir"),
            ((int)UserRole.InternParalegal, "Intern / Paralegal")
        };

        public static string NameOf(int roleId) =>
            All.FirstOrDefault(r => r.Id == roleId).Name ?? "Unknown";

        // Roles a Firm Admin is allowed to assign when creating/editing a
        // user - mirrors the backend fix in RoleHierarchy.CanAssignRole
        // (strictly lower privilege only). SRS: "Firm Admin can create
        // only: Partner, Associate Lawyer, Moharrir, Intern/Paralegal" and
        // "Firm Admin cannot create another Firm Admin." Only FirmAdmin can
        // reach the Create/Edit User pages, so this is the full set every
        // caller of this list needs.
        public static IReadOnlyList<(int Id, string Name)> AssignableByFirmAdmin { get; } =
            All.Where(r => r.Id > (int)UserRole.FirmAdmin).ToList();
    }
}
