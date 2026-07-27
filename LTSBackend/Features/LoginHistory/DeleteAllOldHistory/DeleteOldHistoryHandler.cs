using LTSBackend.Data;
using LTSBackend.Features.LoginHistory.DeleteAllOldHistory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.LoginHistory.Commands.DeleteOldHistory;

/// <summary>
/// Bulk-deletes logged-out login history records older than the given
/// number of days (a retention/cleanup sweep).
///
/// TENANT ISOLATION (fixed - see AppDbContext.cs): previously ran with no
/// FirmID scoping at all, so if this had ever been granted to a Firm Admin
/// it would have permanently deleted every firm's old login records in one
/// call. The global query filter on LoginHistory now scopes this
/// automatically to the caller's own firm; for SuperAdmin (who bypasses the
/// filter by design) it correctly remains a platform-wide retention sweep,
/// which is the intended behaviour for a platform-level operator.
///
/// Currently only reachable by SuperAdmin in practice - see the comment in
/// DeleteLoginHistoryHandler.cs for why DeleteLoginHistory is not granted
/// to Firm Admin by default.
/// </summary>
public class DeleteOldHistoryHandler(AppDbContext context) : IRequestHandler<DeleteOldHistoryCommand, int>
{
    // Removes every (tenant-scoped) logged-out record older than request.Days and returns the count removed.
    public async Task<int> Handle(DeleteOldHistoryCommand request, CancellationToken cancellationToken)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-request.Days);

        var oldHistory = await context.LoginHistories.Where(x => x.IsLoggedOut && x.LoginTime < cutOffDate).ToListAsync(cancellationToken);

        if (oldHistory.Count == 0)
            return 0;

        context.LoginHistories.RemoveRange(oldHistory);
        await context.SaveChangesAsync(cancellationToken);
        return oldHistory.Count;
    }
}