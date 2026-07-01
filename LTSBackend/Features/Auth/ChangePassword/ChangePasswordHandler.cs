using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.ChangePassword;

public class ChangePasswordHandler(
    AppDbContext context,
    IPasswordService passwordService,
    IAuditService auditService)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);

        if (user == null)
            throw new NotFoundException("User not found");

        bool valid = passwordService.VerifyPassword(request.OldPassword, user.PasswordHash);
        if (!valid)
            throw new ValidationException(new List<string> { "Old password incorrect" });

        user.PasswordHash = passwordService.HashPassword(request.NewPassword);

        var auditLog = auditService.Create(user.UserID, "Password Changed");
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}