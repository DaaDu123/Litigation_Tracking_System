using LTSBackend.Comman.Enum;

namespace LTSBackend.Models
{
    public class RoleNames
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string Lawyer = nameof(UserRole.Lawyer);
        public const string Clerk = nameof(UserRole.Clerk);
        public const string Operator = nameof(UserRole.Operator);

        public const string AdminOnly = Admin;
        public const string AdminAndLawyer = Admin + "," + Lawyer;
        public const string AllStaff = Admin + "," + Lawyer + "," + Clerk + "," + Operator;
    }

    public static class RoleExtensions
    {
        public static string ToRoleName(this UserRole role) => role.ToString();

        public static bool TryParseRole(string? roleName, out UserRole role)
            => Enum.TryParse(roleName, ignoreCase: true, out role);
    }
}