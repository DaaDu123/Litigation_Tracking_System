using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(AppDbContext context,IAuditService auditService): IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request,CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);
        if (user == null)
            throw new NotFoundException("User not found");
        user.IsActive = false;
        context.AuditLogs.Add(auditService.Create(user.UserID, "User Deactivated"));
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}