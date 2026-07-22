using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Profile.Queries;

public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, ProfileDTO>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetMyProfileHandler> _logger;

    public GetMyProfileHandler(AppDbContext context, ILogger<GetMyProfileHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProfileDTO> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching profile for user: {UserId}", request.UserID);

        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.UserID == request.UserID,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Profile fetch failed: User not found: {UserId}", request.UserID);
            throw new NotFoundException("User not found.");
        }

        var profile = new ProfileDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            ProfileImage = user.ProfileImage,
            RoleName = user.Role?.RoleName
        };

        _logger.LogInformation("Profile fetched for user: {UserId}", request.UserID);

        return profile;
    }
}