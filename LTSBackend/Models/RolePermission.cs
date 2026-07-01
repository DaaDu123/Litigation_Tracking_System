using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models
{
    public class RolePermission
    {
        public int RoleID { get; set; }
        public int PermissionID { get; set; }

        [ForeignKey(nameof(RoleID))]
        public Role? Role { get; set; }

        [ForeignKey(nameof(PermissionID))]
        public Permission? Permission { get; set; }
    }
}
