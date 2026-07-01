using System.ComponentModel.DataAnnotations;

namespace LTSBackend.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required,MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
