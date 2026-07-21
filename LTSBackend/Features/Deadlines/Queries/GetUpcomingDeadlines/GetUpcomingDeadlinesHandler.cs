using LTSBackend.Data;
using LTSBackend.Features.Deadlines.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Deadlines.Queries.GetUpcomingDeadlines
{
    public class GetUpcomingDeadlinesHandler(AppDbContext _context) : IRequestHandler<GetUpcomingDeadlinesQuery, List<DeadlineDetailDTO>>
    {
        public async Task<List<DeadlineDetailDTO>> Handle(GetUpcomingDeadlinesQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow.Date;

            var deadlines = await _context.Deadlines
                .AsNoTracking()
                .Include(d => d.Case)
                .Where(d => !d.Completed)
                .OrderBy(d => d.DueDate)
                .ToListAsync(cancellationToken);

            var result = deadlines
                .Where(d =>
                {
                    var reminderDate = d.DueDate.Date.AddDays(-(request.DaysAhead ?? d.ReminderDays));
                    return now >= reminderDate; // inside reminder window OR overdue
                })
                .Select(d => new DeadlineDetailDTO
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
                    IsOverdue = d.DueDate.Date < now
                })
                .ToList();

            return result;
        }
    }
}
