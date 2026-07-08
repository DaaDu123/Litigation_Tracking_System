using LTSBackend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.LoginHistory.Commands.DeleteOldHistory;

public class DeleteOldHistoryHandler(AppDbContext context): IRequestHandler<DeleteOldHistoryCommand, int>
{
    public async Task<int> Handle(DeleteOldHistoryCommand request,CancellationToken cancellationToken)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-request.Days);

        var oldHistory = await context.LoginHistories
            .Where(x =>x.IsLoggedOut && x.LoginTime < cutOffDate)
            .ToListAsync(cancellationToken);

        if (!oldHistory.Any())
            return 0;

        context.LoginHistories.RemoveRange(oldHistory);

        await context.SaveChangesAsync(cancellationToken);

        return oldHistory.Count;
    }
}