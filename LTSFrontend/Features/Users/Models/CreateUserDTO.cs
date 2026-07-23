namespace LTSFrontend.Features.Users.Models
{
    /// <summary>
    /// Client-side form model for creating/updating a user. Sent to the
    /// backend as multipart/form-data (CreateUserCommand / UpdateUserCommand
    /// both bind via [FromForm] because of the optional ProfileImage file).
    /// </summary>
    public class CreateUserDTO
    {
        public int? UserID { get; set; } // set when editing
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // ignored on update
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public int? RoleID { get; set; }
        public bool IsActive { get; set; } = true; // used on update only
    }
}
