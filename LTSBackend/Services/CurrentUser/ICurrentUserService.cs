namespace LTSBackend.Services.CurrentUser;

/// <summary>
/// Resolves the acting user's identity + firm scope from the current
/// HTTP request's JWT claims. Used everywhere we need to enforce
/// multi-tenant row-level isolation (a Firm Admin/Partner/etc. must
/// never see another firm's users or cases).
/// SuperAdmin has FirmID == null and IsSuperAdmin == true, meaning
/// "no firm scope restriction" - they can see across all firms.
/// </summary>
public interface ICurrentUserService
{
    int? UserID { get; }
    int? FirmID { get; }
    string? RoleName { get; }
    bool IsSuperAdmin { get; }
    bool IsAuthenticated { get; }
}
