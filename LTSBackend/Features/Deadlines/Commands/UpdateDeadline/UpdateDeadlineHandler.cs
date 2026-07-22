using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Deadlines.Commands.UpdateDeadline
{
    public class UpdateDeadlineHandler(AppDbContext _context,IAuditService _auditService,ICurrentUserService _currentUser,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateDeadlineCommand, bool>
    {
        public async Task<bool> Handle(UpdateDeadlineCommand request, CancellationToken cancellationToken)
        {
            var deadline = await _context.Deadlines
                .Include(d => d.Case)
                .FirstOrDefaultAsync(d => d.DeadlineID == request.Deadline.DeadlineID, cancellationToken);

            if (deadline == null || (!_currentUser.IsSuperAdmin && deadline.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Deadline ID {request.Deadline.DeadlineID} nahi mila");

            if (deadline.Completed)
                throw new ValidationException(new List<string> { "Mukammal (completed) deadline ko update nahi kiya ja sakta" });

            deadline.DeadlineType = request.Deadline.DeadlineType;
            deadline.DueDate = request.Deadline.DueDate;
            deadline.ReminderDays = request.Deadline.ReminderDays;
            deadline.Remarks = request.Deadline.Remarks;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Deadline Updated: DeadlineID {deadline.DeadlineID}"));

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}