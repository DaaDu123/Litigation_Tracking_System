using LTSBackend.Comman.Enum;

namespace LTSBackend.Models.Security;

public static class RoleNames
{
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);
    public const string FirmAdmin = nameof(UserRole.FirmAdmin);
    public const string Partner = nameof(UserRole.Partner);
    public const string AssociateLawyer = nameof(UserRole.AssociateLawyer);
    public const string Moharrir = nameof(UserRole.Moharrir);
    public const string InternParalegal = nameof(UserRole.InternParalegal);

    // ===== POLICY COMBINATIONS =====

    /// <summary>
    /// Super Admin only - system-wide management
    /// </summary>
    public const string SuperAdminOnly = SuperAdmin;

    /// <summary>
    /// Firm Admin + Super Admin - firm and system management
    /// </summary>
    public const string FirmAdminAndAbove = FirmAdmin + "," + SuperAdmin;

    /// <summary>
    /// Partner + Firm Admin + Super Admin - senior management
    /// </summary>
    public const string PartnerAndAbove = Partner + "," + FirmAdmin + "," + SuperAdmin;

    /// <summary>
    /// All lawyers - Partner, Associate, Moharrir with permissions
    /// </summary>
    public const string AllLawyers = Partner + "," + AssociateLawyer + "," + Moharrir;

    /// <summary>
    /// All staff except Super Admin
    /// </summary>
    public const string AllFirmUsers = FirmAdmin + "," + Partner + "," + AssociateLawyer + "," +
                                      Moharrir + "," + InternParalegal;

    /// <summary>
    /// Document viewers - Lawyers and authorized Moharrir
    /// </summary>
    public const string CanViewDocuments = Partner + "," + AssociateLawyer + "," + Moharrir;

    /// <summary>
    /// Case creators - Partner and above
    /// </summary>
    public const string CanCreateCases = Partner + "," + FirmAdmin + "," + SuperAdmin;

    /// <summary>
    /// Case assignment managers - Partner and Firm Admin
    /// </summary>
    public const string CanAssignCases = Partner + "," + FirmAdmin + "," + SuperAdmin;
}