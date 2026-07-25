using LTSBackend.Features.Courts.DTOs;
using MediatR;

namespace LTSBackend.Features.Courts.Queries.GetAllCourts;

/// <summary>
/// Returns all courts. Optional search text filters by court name,
/// type, or jurisdiction (useful for large court lists / dropdown search).
/// </summary>
public sealed record GetAllCourtsQuery(string? SearchText = null) : IRequest<List<CourtDTO>>;
