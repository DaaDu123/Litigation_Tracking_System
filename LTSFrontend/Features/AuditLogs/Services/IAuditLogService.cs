using LTSFrontend.Core.Models;
using LTSFrontend.Features.AuditLogs.Models;

namespace LTSFrontend.Features.AuditLogs.Services
{
    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogDTO>> GetAllAsync(AuditLogFilterDTO filter);
    }
}
