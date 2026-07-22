using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Users.DTOs;
using LTSBackend.Features.Users.Queries.GetUserById;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDTO?>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        AppDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UserDTO?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching user: {UserId}", request.UserID);

        var query = _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.UserID == request.UserID);

        // Multi-tenancy: can't fetch a user outside your own firm.
        if (!_currentUser.IsSuperAdmin)
        {
            query = query.Where(x => x.FirmID == _currentUser.FirmID);
        }

        var user = await query
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