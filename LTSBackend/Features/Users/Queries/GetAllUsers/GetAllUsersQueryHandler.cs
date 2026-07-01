using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;

namespace LTSBackend.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(AppDbContext context)
: IRequestHandler<GetAllUsersQuery, List<UserDTO>>
{
    public async Task<List<UserDTO>> Handle(
    GetAllUsersQuery request,
    CancellationToken cancellationToken)
    {
        return await context.Users
        .AsNoTracking()
        .Where(x => x.IsActive)
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
    }
}
