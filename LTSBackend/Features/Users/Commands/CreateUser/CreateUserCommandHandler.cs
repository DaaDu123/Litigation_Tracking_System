using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Services;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(AppDbContext context,IPasswordService passwordService,
                                IFileService fileService): IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Email validation
        bool emailExists = await context.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (emailExists)
            throw new ValidationException(new() { "Email already exists." });
        if (!request.RoleID.HasValue)
            throw new ValidationException(new() { "RoleID is required." });

        if (!Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
            throw new ValidationException(new() { $"RoleID {request.RoleID} is not a valid role." });

        bool roleExists = await context.Roles.AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);
        if (!roleExists)
            throw new NotFoundException($"Role {request.RoleID} not found");

        // 3. Image Upload Process
        string imageUrl = string.Empty;
        if (request.ProfileImage != null)
        {
            imageUrl = await fileService.SaveFileAsync(request.ProfileImage, "profile_pictures");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordService.HashPassword(request.Password),
            Phone = request.Phone,
            Department = request.Department,
            RoleID = request.RoleID,
            ProfileImage = imageUrl,
            IsActive = true // Admin-created users are active immediately — no OTP step
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.UserID;
    }
}
