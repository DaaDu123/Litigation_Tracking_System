using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Features.Hearings.DTOs;
using LTSBackend.Services.CurrentUser;
using LTSBackend.Services.Permissions;

namespace LTSBackend.Features.Hearings.Queries.GetCaseHearings
{
    public class GetCaseHearingsQueryHandler : IRequestHandler<GetCaseHearingsQuery, PagedHearingResult<HearingDetailDTO>>
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPermissionService _permissionService;

        public GetCaseHearingsQueryHandler(AppDbContext context, ICurrentUserService currentUser, IPermissionService permissionService)
        {
            _context = context;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async Task<PagedHearingResult<HearingDetailDTO>> Handle(GetCaseHearingsQuery request, CancellationToken cancellationToken)
        {
            // SECURITY FIX (IDOR): firm scoping alone let any firm user - including
            // AssociateLawyer/Moharrir/InternParalegal - read hearings for a case
            // they aren't assigned to. Mirrors GetCaseAssignmentsHandler.
            if (!_currentUser.IsSuperAdmin && _currentUser.UserID.HasValue)
            {
                bool hasFullVisibility = await _permissionService.HasFullCaseDirectoryVisibilityAsync(_currentUser.UserID.Value, cancellationToken);
                if (!hasFullVisibility)
                {
                    bool isAssignedToCase = await _permissionService.IsUserAssignedToCaseAsync(_currentUser.UserID.Value, request.CaseId, cancellationToken);
                    if (!isAssignedToCase)
                    {
                        return new PagedHearingResult<HearingDetailDTO> { Items = new List<HearingDetailDTO>(), TotalCount = 0, PageNumber = request.PageNumber, PageSize = request.PageSize };
                    }
                }
            }

            var query = _context.Hearings
                .Include(h => h.Case)
                .Include(h => h.Court)
                .Where(h => h.CaseID == request.CaseId);

            // Multi-tenant isolation
            if (!_currentUser.IsSuperAdmin)
                query = query.Where(h => h.Case.FirmID == _currentUser.FirmID);

            query = query.OrderByDescending(h => h.HearingDate);

            int totalCount = await query.CountAsync(cancellationToken);

            var hearings = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

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