//using LTSBackend.Models.Security;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace LTSBackend.Models.Audit;

//public class AuditLog
//{
//    [Key]
//    public int LogID { get; set; }

//    public int? UserID { get; set; }

//    [Required]
//    [MaxLength(100)]
//    public string Action { get; set; } = string.Empty;

//    [Required]
//    [MaxLength(100)]
//    public string Module { get; set; } = string.Empty;

//    [MaxLength(100)]
//    public string? UserName { get; set; }

//    [MaxLength(150)]
//    public string? Email { get; set; }

//    [MaxLength(100)]
//    public string? EntityName { get; set; }

//    [MaxLength(100)]
//    public string? EntityId { get; set; }

//    [MaxLength(10)]
//    public string? HttpMethod { get; set; }

//    [MaxLength(300)]
//    public string? RequestPath { get; set; }

//    [MaxLength(50)]
//    public string? IPAddress { get; set; }

//    [MaxLength(500)]
//    public string? UserAgent { get; set; }

//    public int StatusCode { get; set; } = 200;

//    public long ExecutionTime { get; set; } = 0;  // Milliseconds

//    [Required]
//    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

//    // Foreign Key & Navigation Property
//    [ForeignKey(nameof(UserID))]
//    public User? User { get; set; }
//}