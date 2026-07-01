using MediatR;
using Microsoft.AspNetCore.Http;
namespace LTSBackend.Features.Users.Commands.CreateUser;
public record CreateUserCommand(string FullName,string Email,string Password,string? Phone,string? Department,int? RoleID,
    IFormFile? ProfileImage
) : IRequest<int>;