using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Roles.Commands.DeleteRole;

public class DeleteRoleHandler(AppDbContext context) : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request,CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.RoleID == request.RoleID,cancellationToken);

        if (role == null)
            throw new NotFoundException("Role not found.");

        if (role.Users.Count > 0)
            throw new ValidationException(["Role is assigned to users and cannot be deleted."]);

        context.Roles.Remove(role);

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}