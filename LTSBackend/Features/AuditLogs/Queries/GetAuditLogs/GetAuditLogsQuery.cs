using FluentValidation;
using LTSBackend.Comman.Responses;
using LTSBackend.Data;
using LTSBackend.Features.AuditLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    string? Search,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Action,
    int PageNumber,
    int PageSize
) : IRequest<PagedResult<AuditLogDTO>>;
