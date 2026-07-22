using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseAssignments.Commands.EndAssignment
{
    /// <summary>
    /// "Reassign" per SRS UC-04 is implemented as: End existing assignment (soft) + AssignCase (new).
    /// We use a soft end (EndDate) instead of hard delete to preserve historical accountability
    /// (SRS Section 5.8 Auditability: "Maintain accountability and ownership of cases").
    /// </summary>
    public class EndAssignmentHandler(
        AppDbContext _context,
        IAuditService _auditService,
        ICurrentUserService _currentUser,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<EndAssignmentCommand, bool>
    {
        public async Task<bool> Handle(EndAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.CaseAssignments
                .Include(a => a.Case)
                .FirstOrDefaultAsync(a => a.AssignmentID == request.AssignmentID, cancellationToken);

            if (assignment == null || (!_currentUser.IsSuperAdmin && assignment.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Assignment ID {request.AssignmentID} nahi mila");

            if (assignment.EndDate != null)
                throw new ValidationException(new List<string> { "Yeh assignment pehle se hi khatam ho chuki hai" });

            assignment.EndDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Remarks))
                assignment.Remarks = request.Remarks;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Assignment Ended: AssignmentID {assignment.AssignmentID}"));

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