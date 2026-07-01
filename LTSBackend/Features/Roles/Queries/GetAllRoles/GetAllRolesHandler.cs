using LTSBackend.Data;
using LTSBackend.Features.Roles.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace LTSBackend.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesHandler(AppDbContext context): IRequestHandler<GetAllRolesQuery, List<RoleDTO>>
{
    public async Task<List<RoleDTO>> Handle(GetAllRolesQuery request,CancellationToken cancellationToken)
    {
        return await context.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Select(x => new RoleDTO
            {
                RoleID = x.RoleID,
                RoleName = x.RoleName,
                Description = x.Description,
                Permissions = x.RolePermissions
                    .Select(rp => new RolePermissionDTO
                    {
                        PermissionID = rp.PermissionID,
                        PermissionName = rp.Permission!.PermissionName
                    }).ToList()
            }).ToListAsync(cancellationToken);
    }
}