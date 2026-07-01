using LTSBackend.Comman.Enum;
using System.ComponentModel.DataAnnotations;
namespace LTSBackend.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? ProfileImage { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(100)]
        public string? Department { get; set; }
        public int? RoleID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Role? Role { get; set; }
        public UserRole? GetRole() => RoleID.HasValue && Enum.IsDefined(typeof(UserRole), RoleID.Value) ? (UserRole)RoleID.Value : null;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<UserOtp> UserOtps { get; set; } = new List<UserOtp>();
    }
}