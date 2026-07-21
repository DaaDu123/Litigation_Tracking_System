using LTSBackend.Features.Dashboard.DTO.cs;

namespace LTSBackend.Features.Dashboard.DTOs;

public class DashboardDTO
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalPermissions { get; set; }
    public int TotalAuditLogs { get; set; }
    public int TotalRefreshTokens { get; set; }
    public List<RecentActivityDTO> RecentActivities { get; set; } = [];
}