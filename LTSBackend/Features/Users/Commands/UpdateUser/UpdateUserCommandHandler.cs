using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(AppDbContext context, IFileService fileService, IAuditService auditService)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);
        if (user == null)
            throw new NotFoundException("User not found");

        // Email Exists check
        bool emailExists = await context.Users.AnyAsync(x =>
            x.Email == request.Email &&
            x.UserID != request.UserID,
            cancellationToken);

        if (emailExists)
            throw new ValidationException(new() { "Email already exists." });

        if (!request.RoleID.HasValue)
            throw new ValidationException(new() { "RoleID is required." });

        if (!Enum.IsDefined(typeof(UserRole), request.RoleID.Value))
            throw new ValidationException(new() { $"RoleID {request.RoleID} is not a valid role." });

        bool roleExists = await context.Roles.AnyAsync(x => x.RoleID == request.RoleID, cancellationToken);
        if (!roleExists)
            throw new NotFoundException($"Role {request.RoleID} not found");

        // Image Update Process
        if (request.ProfileImage != null)
        {
            fileService.DeleteFile(user.ProfileImage);
            user.ProfileImage = await fileService.SaveFileAsync(request.ProfileImage, "profile_pictures");
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Department = request.Department;
        user.RoleID = request.RoleID;
        user.IsActive = request.IsActive;

        // Audit trail — keep this consistent with create/delete/login actions.
        context.AuditLogs.Add(auditService.Create(user.UserID, "User Updated"));

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}