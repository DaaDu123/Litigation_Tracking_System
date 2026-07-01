using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LTSBackend.Models
{
    public class UserOtp
    {
        [Key]
        public int OtpID { get; set; }
        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
    }
}