using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Deadlines.Commands.CompleteDeadline
{
    public class CompleteDeadlineHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<CompleteDeadlineCommand, bool>
    {
        public async Task<bool> Handle(CompleteDeadlineCommand request, CancellationToken cancellationToken)
        {
            var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.DeadlineID == request.DeadlineID, cancellationToken);
            if (deadline == null)
                throw new NotFoundException($"Deadline ID {request.DeadlineID} nahi mila");

            deadline.Completed = true;
            deadline.CompletedDate = DateTime.UtcNow;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Deadline Completed: DeadlineID {deadline.DeadlineID}"));

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
