namespace LTSFrontend.Features.Dashboard.Models
{
    /// <summary>Mirrors LTSBackend.Features.Dashboard.DTOs.DashboardDTO</summary>
    public class DashboardDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalPermissions { get; set; }
        public int TotalAuditLogs { get; set; }
        public int TotalRefreshTokens { get; set; }
        public List<RecentActivityDTO> RecentActivities { get; set; } = new();
    }
}
