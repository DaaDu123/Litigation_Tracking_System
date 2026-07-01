using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Roles.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Roles.Queries.GetRoleById;

public class GetRoleByIdHandler(AppDbContext context): IRequestHandler<GetRoleByIdQuery, RoleDTO>
{
    public async Task<RoleDTO> Handle(GetRoleByIdQuery request,CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.RoleID == request.RoleID,cancellationToken);

        if (role == null)
            throw new NotFoundException("Role not found.");

        return new RoleDTO
        {
            RoleID = role.RoleID,
            RoleName = role.RoleName,
            Description = role.Description,
            Permissions = role.RolePermissions
                .Select(rp => new RolePermissionDTO
                {
                    PermissionID = rp.PermissionID,
                    PermissionName = rp.Permission!.PermissionName
                }).ToList()
        };
    }
}