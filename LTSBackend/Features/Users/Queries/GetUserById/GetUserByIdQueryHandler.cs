using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetUserByIdQueryHandler (AppDbContext _context, ILogger<GetUserByIdQueryHandler> _logger) : IRequestHandler<GetUserByIdQuery, UserDTO?>
{
    public async Task<UserDTO?> Handle(GetUserByIdQuery request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching user: {UserId}", request.UserID);

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
                RoleName = x.Role != null ? x.Role.RoleName : null,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found or is inactive: {UserId}", request.UserID);
            throw new NotFoundException($"User with ID {request.UserID} not found.");
        }

        _logger.LogInformation("User retrieved: {UserId}", request.UserID);

        return user;
    }
}