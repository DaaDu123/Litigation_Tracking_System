namespace LTSFrontend.Features.AuditLogs.DTOs
{
    /// <summary>Mirrors LTSBackend.Features.AuditLogs.DTOs.PagedAuditLogResponseDTO</summary>
    public class PagedAuditLogResponseDTO
    {
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<AuditLogDTO> Records { get; set; } = new();
    }
}
