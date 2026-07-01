using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Roles.Commands.CreateRole;

public class CreateRoleHandler(AppDbContext context): IRequestHandler<CreateRoleCommand, int>
{
    public async Task<int> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        bool exists = await context.Roles.AnyAsync(x => x.RoleName == request.RoleName,cancellationToken);

        if (exists)
            throw new ValidationException(["Role already exists."]);

        var role = new Role
        {
            RoleName = request.RoleName,
            Description = request.Description
        };

        context.Roles.Add(role);

        await context.SaveChangesAsync(cancellationToken);

        foreach (var permissionId in request.PermissionIds)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleID = role.RoleID,
                PermissionID = permissionId
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return role.RoleID;
    }
}