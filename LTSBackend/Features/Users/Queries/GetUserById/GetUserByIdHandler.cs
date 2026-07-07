using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Queries.GetUserById;

public class GetUserByIdHandler (AppDbContext _context, ILogger<GetUserByIdHandler> _logger) : IRequestHandler<GetUserByIdQuery, UserDTO?>
{    
    public async Task<UserDTO?> Handle(GetUserByIdQuery request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching user: {UserID}", request.UserID);

        var user = await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.UserID == request.UserID)
            .Include(x => x.Role)
            .Select(x => new UserDTO
            {
                UserID = x.UserID,
                FullName = x.FullName,
                Email = x.Email,
                ProfileImage = x.ProfileImage,
                Phone = x.Phone,
                Department = x.Department,
                RoleID = x.RoleID,
                RoleName = x.Role != null ? x.Role.RoleName : "No Role",
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found or is inactive: {UserID}", request.UserID);
            throw new NotFoundException($"User ID {request.UserID} nahi mila");
        }

        _logger.LogInformation("User successfully fetched: {UserID}", request.UserID);

        return user;
    }
}