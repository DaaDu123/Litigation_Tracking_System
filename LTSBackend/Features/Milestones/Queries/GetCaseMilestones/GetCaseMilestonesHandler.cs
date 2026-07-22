using LTSBackend.Data;
using LTSBackend.Features.Milestones.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Milestones.Queries.GetCaseMilestones
{
    public class GetCaseMilestonesHandler(AppDbContext _context, ICurrentUserService _currentUser) : IRequestHandler<GetCaseMilestonesQuery, List<MilestoneDetailDTO>>
    {
        public async Task<List<MilestoneDetailDTO>> Handle(GetCaseMilestonesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.CaseMilestones
                .AsNoTracking()
                .Include(m => m.Case)
                .Where(m => m.CaseID == request.CaseID);

            // FIX: multi-tenant isolation
            if (!_currentUser.IsSuperAdmin)
                query = query.Where(m => m.Case.FirmID == _currentUser.FirmID);

            var milestones = await query
                .OrderBy(m => m.MilestoneDate)
                .ToListAsync(cancellationToken);

            var completedByIds = milestones.Where(m => m.CompletedDate != null).Select(m => m.CompletedBy).Distinct().ToList();
            var names = await _context.Users
                .Where(u => completedByIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.FullName, cancellationToken);

            return milestones.Select(m => new MilestoneDetailDTO
            {
                MilestoneID = m.MilestoneID,
                CaseID = m.CaseID,
                CaseNumber = m.Case?.CaseNumber,
                Milestone = m.Milestone,
                MilestoneDate = m.MilestoneDate,
                Description = m.Description,
                IsCompleted = m.CompletedDate != null,
                CompletedByName = m.CompletedDate != null && names.TryGetValue(m.CompletedBy, out var n) ? n : null,
                CompletedDate = m.CompletedDate
            }).ToList();
        }
    }
}