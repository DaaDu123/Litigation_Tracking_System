using MediatR;

namespace LTSBackend.Features.Permissions.Commands.AssignPermissions;

public record AssignPermissionsCommand(
    int RoleID,
    List<int> PermissionIds
) : IRequest<bool>;