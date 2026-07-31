using LTSBackend.Features.Dashboard.DTO.cs;

namespace LTSBackend.Features.Dashboard.DTOs;

/// <summary>
/// SuperAdmin's own dashboard - platform-owner scope only (Roles SRS
/// §4.I: workspace provisioning, data custody, domain migration,
/// immutable audit logging). Deliberately contains NO case, document,
/// hearing, or firm-internal user-management data - that is FirmAdmin's
/// dashboard, not this one. No other role can request this DTO
/// (DashboardController routes by the caller's own role only).
/// </summary>
public class SuperAdminDashboardDTO
{
    public int TotalFirms { get; set; }
    public int ActiveFirms { get; set; }
    public int BlockedFirms { get; set; }
    public int PendingDomainMigrations { get; set; }

    public int TotalFirmAdmins { get; set; }

    public int TotalSystemAuditLogs { get; set; }
    public int TotalActiveSessions { get; set; }

    public List<RecentActivityDTO> RecentActivities { get; set; } = [];
}
