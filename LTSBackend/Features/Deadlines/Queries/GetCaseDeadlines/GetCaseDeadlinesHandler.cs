using LTSBackend.Data;
using LTSBackend.Features.Deadlines.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Deadlines.Queries.GetCaseDeadlines
{
    public class GetCaseDeadlinesHandler(AppDbContext _context, ICurrentUserService _currentUser) : IRequestHandler<GetCaseDeadlinesQuery, List<DeadlineDetailDTO>>
    {
        public async Task<List<DeadlineDetailDTO>> Handle(GetCaseDeadlinesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Deadlines.AsNoTracking().Where(d => d.CaseID == request.CaseID);

            // FIX: multi-tenant isolation
            if (!_currentUser.IsSuperAdmin)
                query = query.Where(d => d.Case.FirmID == _currentUser.FirmID);

            if (request.Completed.HasValue)
                query = query.Where(d => d.Completed == request.Completed.Value);

            var deadlines = await query
                .Include(d => d.Case)
                .OrderBy(d => d.DueDate)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow.Date;

            return deadlines.Select(d => new DeadlineDetailDTO
            {
                DeadlineID = d.DeadlineID,
                CaseID = d.CaseID,
                CaseNumber = d.Case?.CaseNumber,
                CaseTitle = d.Case?.CaseTitle,
                DeadlineType = d.DeadlineType,
                DueDate = d.DueDate,
                ReminderDays = d.ReminderDays,
                Completed = d.Completed,
                CompletedDate = d.CompletedDate,
                Remarks = d.Remarks,
                DaysRemaining = (int)(d.DueDate.Date - now).TotalDays,
                IsOverdue = !d.Completed && d.DueDate.Date < now
            }).ToList();
        }
    }
}