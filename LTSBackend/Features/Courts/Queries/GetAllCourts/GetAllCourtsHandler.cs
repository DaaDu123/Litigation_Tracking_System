using LTSBackend.Data;
using LTSBackend.Features.Courts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Courts.Queries.GetAllCourts;

public sealed class GetAllCourtsHandler(AppDbContext _context, ILogger<GetAllCourtsHandler> _logger) : IRequestHandler<GetAllCourtsQuery, List<CourtDTO>>
{
    public async Task<List<CourtDTO>> Handle(GetAllCourtsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all courts (SearchText={SearchText})", request.SearchText);

        var query = _context.Courts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText.Trim().ToLower();
            query = query.Where(x =>
                x.CourtName.ToLower().Contains(search) ||
                x.CourtType.ToLower().Contains(search) ||
                x.Jurisdiction.ToLower().Contains(search));
        }

        var courts = await query.OrderBy(x => x.CourtName)
            .Select(x => new CourtDTO
            {
                CourtID = x.CourtID,
                CourtName = x.CourtName,
                CourtType = x.CourtType,
                Jurisdiction = x.Jurisdiction,
                Address = x.Address
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} courts", courts.Count);

        return courts;
    }
}
