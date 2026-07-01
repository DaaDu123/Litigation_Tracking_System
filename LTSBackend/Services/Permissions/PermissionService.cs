using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.Permissions;

public class PermissionService (AppDbContext _context) : IPermissionService
{

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);

        if (user?.Role == null)
            return false;

        return user.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.PermissionName == permission);
    }
}