using LTSBackend.Comman.Responses;
using LTSBackend.Data;
using LTSBackend.Features.AuditLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDTO>>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetAuditLogsHandler> _logger;

    public GetAuditLogsHandler(AppDbContext context, ILogger<GetAuditLogsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<AuditLogDTO>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching audit logs - Page: {PageNumber}, Size: {PageSize}",
            request.PageNumber,
            request.PageSize);

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        // ================================================
        // 1. Search by user name or email
        // ================================================
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.User != null &&
                (x.User.FullName.Contains(search) ||
                 x.User.Email.Contains(search)));

            _logger.LogInformation("Applied search filter: {Search}", search);
        }

        // ================================================
        // 2. Filter by action
        // ================================================
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(x =>
                x.Action == request.Action.Trim());

            _logger.LogInformation("Applied action filter: {Action}", request.Action);
        }

        // ================================================
        // 3. Filter by date range
        // ================================================
        if (request.FromDate.HasValue)
        {
            query = query.Where(x =>
                x.Timestamp >= request.FromDate.Value);

            _logger.LogInformation("Applied from date filter: {FromDate}", request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x =>
                x.Timestamp <= request.ToDate.Value);

            _logger.LogInformation("Applied to date filter: {ToDate}", request.ToDate);
        }

        // ================================================
        // 4. Get total record count
        // ================================================
        var total = await query.CountAsync(cancellationToken);

        // ================================================
        // 5. Apply pagination and ordering
        // ================================================
        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AuditLogDTO
            {
                LogID = x.LogID,
                UserID = x.UserID,
                UserName = x.User != null ? x.User.FullName : "System",
                Action = x.Action,
                IPAddress = x.IPAddress,
                Timestamp = x.Timestamp
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Fetched {Count} audit logs of {Total} total",
            items.Count,
            total);

        return new PagedResult<AuditLogDTO>
        {
            Items = items,
            TotalRecords = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}