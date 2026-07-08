using MediatR;
using System.ComponentModel.DataAnnotations;
namespace LTSBackend.Features.Profile.Commands;

public record UpdateMyProfileCommand(
    [Required(ErrorMessage = "Full name is required")]
    string FullName,
    [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    string? Phone,
    [MaxLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    string? Department,
    IFormFile? ProfileImage
) : IRequest<bool>
{
    public int UserID { get; init; }
}