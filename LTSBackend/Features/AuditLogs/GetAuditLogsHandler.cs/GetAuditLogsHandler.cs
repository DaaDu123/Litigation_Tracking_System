using LTSBackend.Data;
using LTSBackend.Features.AuditLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.AuditLogs.Queries;

public class GetAuditLogsHandler(AppDbContext context): IRequestHandler<GetAuditLogsQuery, List<AuditLogDTO>>
{
    public async Task<List<AuditLogDTO>> Handle(GetAuditLogsQuery request,CancellationToken cancellationToken)
    {
        return await context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new AuditLogDTO
            {
                LogID = x.LogID,
                UserID = x.UserID,
                Action = x.Action,
                IPAddress = x.IPAddress,
                Timestamp = x.Timestamp
            })
            .ToListAsync(cancellationToken);
    }
}