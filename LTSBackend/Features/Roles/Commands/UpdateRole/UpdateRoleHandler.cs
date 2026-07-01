using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Roles.Commands.UpdateRole;

public class UpdateRoleHandler(AppDbContext context)
    : IRequestHandler<UpdateRoleCommand, bool>
{
    public async Task<bool> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(
                x => x.RoleID == request.RoleID,
                cancellationToken);

        if (role == null)
            throw new NotFoundException("Role not found.");

        // Prevent renaming to a name already used by a different role.
        bool nameTaken = await context.Roles.AnyAsync(
            x => x.RoleName == request.RoleName && x.RoleID != request.RoleID,
            cancellationToken);

        if (nameTaken)
            throw new ValidationException(["Role name already exists."]);

        role.RoleName = request.RoleName;
        role.Description = request.Description;

        context.RolePermissions.RemoveRange(role.RolePermissions);

        role.RolePermissions = request.PermissionIds
            .Distinct()
            .Select(x => new RolePermission
            {
                RoleID = role.RoleID,
                PermissionID = x
            })
            .ToList();

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}