using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Profile.Commands;

public class UpdateMyProfileHandler(AppDbContext context,IFileService fileService) : IRequestHandler<UpdateMyProfileCommand, bool>
{
    public async Task<bool> Handle(UpdateMyProfileCommand request,CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.UserID == request.UserID,cancellationToken);

        if (user == null)
            throw new NotFoundException("User not found.");

        if (request.ProfileImage != null)
        {
            fileService.DeleteFile(user.ProfileImage);

            user.ProfileImage =await fileService.SaveFileAsync(request.ProfileImage,"profile_pictures");
        }

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.Department = request.Department;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}