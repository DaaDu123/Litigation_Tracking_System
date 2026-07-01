using LTSBackend.Data;
using LTSBackend.Features.Dashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Dashboard.Queries;

public class GetDashboardStatsHandler(AppDbContext context): IRequestHandler<GetDashboardStatsQuery, DashboardDTO>
{
    public async Task<DashboardDTO> Handle(GetDashboardStatsQuery request,CancellationToken cancellationToken)
    {
        var dto = new DashboardDTO
        {
            TotalUsers = await context.Users.CountAsync(cancellationToken),

            ActiveUsers = await context.Users
                .CountAsync(x => x.IsActive, cancellationToken),

            TotalRoles = await context.Roles
                .CountAsync(cancellationToken),

            TotalPermissions = await context.Permissions
                .CountAsync(cancellationToken),

            TotalAuditLogs = await context.AuditLogs
                .CountAsync(cancellationToken),

            TotalRefreshTokens = await context.RefreshTokens
                .CountAsync(cancellationToken),

            RecentActivities = await context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .Take(10)
                .Select(x => new RecentActivityDTO
                {
                    LogID = x.LogID,
                    UserID = x.UserID,
                    Action = x.Action,
                    Timestamp = x.Timestamp
                })
                .ToListAsync(cancellationToken)
        };

        return dto;
    }
}