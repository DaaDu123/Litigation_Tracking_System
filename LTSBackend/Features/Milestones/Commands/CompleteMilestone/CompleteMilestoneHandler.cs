using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Milestones.Commands.CompleteMilestone
{
    public class CompleteMilestoneHandler(
        AppDbContext _context,
        IAuditService _auditService,
        ICurrentUserService _currentUser,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<CompleteMilestoneCommand, bool>
    {
        public async Task<bool> Handle(CompleteMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await _context.CaseMilestones
                .Include(m => m.Case)
                .FirstOrDefaultAsync(m => m.MilestoneID == request.MilestoneID, cancellationToken);

            if (milestone == null || (!_currentUser.IsSuperAdmin && milestone.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Milestone ID {request.MilestoneID} nahi mila");

            int currentUserId = GetCurrentUserId();

            milestone.CompletedBy = currentUserId;
            milestone.CompletedDate = DateTime.UtcNow;

            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Milestone Completed: MilestoneID {milestone.MilestoneID}"));

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