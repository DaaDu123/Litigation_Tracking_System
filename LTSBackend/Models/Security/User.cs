using LTSBackend.Comman.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Security;

public class User
{
    [Key]
    public int UserID { get; set; }
    [Required, MaxLength(50)]
    public string EmployeeNo { get; set; } = string.Empty;
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? ProfileImage { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(100)]
    public string? Department { get; set; }
    [MaxLength(100)]
    public string? Designation { get; set; }
    public int? RoleID { get; set; }
    public bool IsExternal { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? LastLogin { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? PasswordChangedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    // Foreign Keys & Navigation Properties
    [ForeignKey(nameof(RoleID))]
    public Role? Role { get; set; }
    // Collections
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserOtp> UserOtps { get; set; } = [];
    public ICollection<LoginHistory> LoginHistories { get; set; } = [];
    /// <summary>
    /// Gets the user's role as an enum, or null if not defined.
    /// </summary>
    public UserRole? GetRole() =>
        RoleID.HasValue && Enum.IsDefined(typeof(UserRole), RoleID.Value)
            ? (UserRole)RoleID.Value
            : null;
}