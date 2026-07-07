using LTSBackend.Data;
using LTSBackend.Features.Dashboard.DTOs;
using MediatR;

namespace LTSBackend.Features.Dashboard.Queries;

public record GetDashboardStatsQuery: IRequest<DashboardDTO>;
