using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetAllUsers;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetAllUsersQueryHandler(AppDbContext _context,ICurrentUserService _currentUser,
    ILogger<GetAllUsersQueryHandler> _logger) : IRequestHandler<GetAllUsersQuery, List<UserDTO>>
{   
    public async Task<List<UserDTO>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all active users");

        var query = _context.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);

        // Multi-tenancy: firm-scoped. SuperAdmin cannot reach this endpoint at all
        // (route-level [Authorize] excludes it - user directory is FirmAdmin's job).
            query = query.Where(x => x.FirmID == _currentUser.FirmID);

        var users = await query
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .Select(x => new UserDTO
            {
                UserID = x.UserID,
                FullName = x.FullName,
                Email = x.Email,
                ProfileImage = x.ProfileImage,
                Phone = x.Phone,
                Department = x.Department,
                RoleID = x.RoleID,
                RoleName = x.Role != null ? x.Role.RoleName : null,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} active users", users.Count);

        return users;
    }
}