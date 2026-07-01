using MediatR;
namespace LTSBackend.Features.Roles.Commands.CreateRole;
public record CreateRoleCommand(string RoleName,string? Description,List<int> PermissionIds) : IRequest<int>;