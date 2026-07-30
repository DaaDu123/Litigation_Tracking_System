namespace LTSFrontend.Features.Roles.Models
{
    /// <summary>Mirrors LTSBackend.Features.Roles.DTOs.RolePermissionDTO</summary>
    public class RolePermissionDTO
    {
        public int PermissionID { get; set; }
        public string PermissionName { get; set; } = string.Empty;
    }
}
