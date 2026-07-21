using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseAssignments.Commands.UpdateAssignment
{
    public class UpdateAssignmentHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateAssignmentCommand, bool>
    {
        public async Task<bool> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.CaseAssignments.FirstOrDefaultAsync(a => a.AssignmentID == request.Assignment.AssignmentID, cancellationToken);
            if (assignment == null)
                throw new NotFoundException($"Assignment ID {request.Assignment.AssignmentID} nahi mila");

            if (assignment.EndDate != null)
                throw new ValidationException(new List<string> { "Khatam ho chuki (ended) assignment ko update nahi kiya ja sakta" });

            assignment.AssignmentType = request.Assignment.AssignmentType;
            assignment.LeadCounsel = request.Assignment.LeadCounsel;
            assignment.Remarks = request.Assignment.Remarks;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Assignment Updated: AssignmentID {assignment.AssignmentID}"));

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
