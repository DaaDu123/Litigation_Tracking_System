using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models
{
    public class Permission
    {
        [Key]
        public int PermissionID { get; set; }
        [Required,MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
