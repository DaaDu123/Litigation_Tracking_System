namespace LTSBackend.Features.Users.DTOs
{
    public class CreateUserDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public int? RoleID { get; set; }
    }
}
