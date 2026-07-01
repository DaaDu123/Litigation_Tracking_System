using MediatR;
namespace LTSBackend.Features.Roles.Commands.DeleteRole;
public record DeleteRoleCommand(int RoleID) : IRequest<bool>;