using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Permissions.Commands.AssignPermissions;

public class AssignPermissionsHandler(AppDbContext context)
    : IRequestHandler<AssignPermissionsCommand, bool>
{
    public async Task<bool> Handle(
        AssignPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(
                x => x.RoleID == request.RoleID,
                cancellationToken);

        if (role == null)
            throw new NotFoundException("Role not found.");

        var validPermissions = await context.Permissions
            .Where(x => request.PermissionIds.Contains(x.PermissionID))
            .Select(x => x.PermissionID)
            .ToListAsync(cancellationToken);

        if (validPermissions.Count != request.PermissionIds.Count)
            throw new ValidationException(
                new()
                {
                    "One or more permissions are invalid."
                });

        context.RolePermissions.RemoveRange(role.RolePermissions);

        var rolePermissions = request.PermissionIds
            .Distinct()
            .Select(permissionId => new RolePermission
            {
                RoleID = request.RoleID,
                PermissionID = permissionId
            });

        await context.RolePermissions.AddRangeAsync(
            rolePermissions,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}