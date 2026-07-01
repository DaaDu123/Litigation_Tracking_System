using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Comman.Exceptions;

namespace LTSBackend.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(AppDbContext context) : IRequestHandler<GetUserByIdQuery, UserDTO?>
{
    public async Task<UserDTO?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(x => x.Role)
            .Where(x => x.IsActive && x.UserID == request.UserID)
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
            }).FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            throw new NotFoundException($"User {request.UserID} not found");

        return user;
    }
}
