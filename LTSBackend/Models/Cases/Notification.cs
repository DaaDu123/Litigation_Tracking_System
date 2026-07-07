using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Cases;

[Table("Notifications")]
public class Notification
{
    [Key]
    public long NotificationID { get; set; }

    [Required]
    public int UserID { get; set; }

    public long? CaseID { get; set; }

    [Required, MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty; // Hearing, Deadline, System

    [Required, MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CaseID))]
    public Case? Case { get; set; }
}