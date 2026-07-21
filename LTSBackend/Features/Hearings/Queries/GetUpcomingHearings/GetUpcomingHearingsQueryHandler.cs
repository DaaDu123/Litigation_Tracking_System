using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Features.Hearings.DTOs;

namespace LTSBackend.Features.Hearings.Queries.GetUpcomingHearings
{
    public class GetUpcomingHearingsQueryHandler (AppDbContext _context) : IRequestHandler<GetUpcomingHearingsQuery, PagedHearingResult<HearingDetailDTO>>
    {
        public async Task<PagedHearingResult<HearingDetailDTO>> Handle(GetUpcomingHearingsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Hearings.Include(h => h.Case).Include(h => h.Court).Where(h => h.HearingDate >= DateTime.UtcNow);

            if (request.CaseId.HasValue)
                query = query.Where(h => h.CaseID == request.CaseId.Value);

            if (request.CourtId.HasValue)
                query = query.Where(h => h.CourtID == request.CourtId.Value);

            query = query.OrderBy(h => h.HearingDate);

            int totalCount = await query.CountAsync(cancellationToken);

            var hearings = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Fetch creator names in one batch (avoid N+1 queries)
            var creatorIds = hearings.Select(h => h.CreatedBy).Distinct().ToList();
            var creatorNames = await _context.Users
                .Where(u => creatorIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.FullName, cancellationToken);

            var hearingDTOs = hearings.Select(h =>
            {
                int daysRemaining = (int)(h.HearingDate - DateTime.UtcNow).TotalDays;
                string priority = daysRemaining <= 1 ? "Critical" :
                                daysRemaining <= 7 ? "High" :
                                daysRemaining <= 15 ? "Medium" : "Normal";

                return new HearingDetailDTO
                {
                    HearingId = h.HearingID,
                    CaseId = h.CaseID,
                    CaseNumber = h.Case?.CaseNumber,
                    CaseTitle = h.Case?.CaseTitle,
                    CourtId = h.CourtID,
                    CourtName = h.Court?.CourtName,
                    HearingDate = h.HearingDate,
                    CourtRoom = h.CourtRoom,
                    JudgeName = h.JudgeName,
                    HearingPurpose = h.Purpose,
                    HearingOutcome = h.Outcome,
                    NextHearingDate = h.NextHearingDate,
                    Remarks = h.Remarks,
                    CreatedByUser = creatorNames.TryGetValue(h.CreatedBy, out var name) ? name : null,
                    CreatedDate = h.CreatedDate,
                    DaysRemaining = daysRemaining,
                    HearingPriority = priority
                };
            }).ToList();

            return new PagedHearingResult<HearingDetailDTO>
            {
                Items = hearingDTOs,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}