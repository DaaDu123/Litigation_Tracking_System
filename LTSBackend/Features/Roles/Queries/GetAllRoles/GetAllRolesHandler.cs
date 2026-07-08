using LTSBackend.Data;
using LTSBackend.Features.Roles.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace LTSBackend.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesHandler : IRequestHandler<GetAllRolesQuery, List<RoleDTO>>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetAllRolesHandler> _logger;

    public GetAllRolesHandler(AppDbContext context, ILogger<GetAllRolesHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleDTO>> Handle(
        GetAllRolesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all roles");

        var roles = await _context.Roles
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
                    })
                    .ToList()
            })
            .OrderBy(x => x.RoleName)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} roles", roles.Count);

        return roles;
    }
}