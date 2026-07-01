using LTSBackend.Features.AuditLogs.DTOs;
using MediatR;
namespace LTSBackend.Features.AuditLogs.Queries;
public record GetAuditLogsQuery(): IRequest<List<AuditLogDTO>>;