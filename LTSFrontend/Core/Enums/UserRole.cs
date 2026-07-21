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
    }
}
