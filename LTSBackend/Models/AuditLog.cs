using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }
        public int? UserID { get; set; }
        [MaxLength(255)]
        public string? Action { get; set; }
        [MaxLength(50)]
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
