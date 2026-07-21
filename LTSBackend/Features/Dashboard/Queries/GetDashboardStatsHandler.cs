using LTSBackend.Data;
using LTSBackend.Features.Dashboard.DTO.cs;
using LTSBackend.Features.Dashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Dashboard.Queries;

public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardDTO>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetDashboardStatsHandler> _logger;

    public GetDashboardStatsHandler(
        AppDbContext context,
        ILogger<GetDashboardStatsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardDTO> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dashboard statistics");

        // ================================================
        // Fetch all required statistics
        // ================================================
        var dto = new DashboardDTO
        {
            TotalUsers = await _context.Users
                .CountAsync(cancellationToken),

            ActiveUsers = await _context.Users
                .CountAsync(x => x.IsActive && !x.IsDeleted, cancellationToken),

            TotalRoles = await _context.Roles
                .CountAsync(cancellationToken),

            TotalPermissions = await _context.Permissions
                .CountAsync(cancellationToken),

            TotalAuditLogs = await _context.AuditLogs
                .CountAsync(cancellationToken),

            // ================================================
            // FIX: previously only checked !IsRevoked, which also
            // counts tokens that have naturally expired but were
            // never explicitly revoked (nothing in the codebase
            // flips IsRevoked on natural expiry — only explicit
            // logout / refresh-rotation / password-reset do that).
            // Added ExpiryDate check so this reflects genuinely
            // active sessions.
            // ================================================
            TotalRefreshTokens = await _context.RefreshTokens
                .CountAsync(x => !x.IsRevoked && x.ExpiryDate > DateTime.UtcNow, cancellationToken),

            RecentActivities = await _context.AuditLogs
                .AsNoTracking()
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

        _logger.LogInformation(
            "Dashboard stats fetched: {TotalUsers} users, {ActiveUsers} active, {TotalRoles} roles, {TotalPermissions} permissions",
            dto.TotalUsers,
            dto.ActiveUsers,
            dto.TotalRoles,
            dto.TotalPermissions);

        return dto;
    }
}