using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Profile.Queries;

public class GetMyProfileHandler(AppDbContext context): IRequestHandler<GetMyProfileQuery, ProfileDTO>
{
    public async Task<ProfileDTO> Handle(GetMyProfileQuery request,CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.UserID == request.UserID,cancellationToken);

        if (user == null)
            throw new NotFoundException("User not found.");

        return new ProfileDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            ProfileImage = user.ProfileImage,
            RoleName = user.Role?.RoleName
        };
    }
}