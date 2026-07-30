namespace LTSFrontend.Features.AuditLogs.Models
{
    /// <summary>Mirrors LTSBackend.Features.AuditLogs.DTOs.AuditLogDTO</summary>
    public class AuditLogDTO
    {
        public int LogID { get; set; }
        public int? UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
