using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Milestones.Commands.DeleteMilestone
{
    public class DeleteMilestoneHandler(AppDbContext _context,IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<DeleteMilestoneCommand, bool>
    {
        public async Task<bool> Handle(DeleteMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await _context.CaseMilestones.FirstOrDefaultAsync(m => m.MilestoneID == request.MilestoneID, cancellationToken);
            if (milestone == null)
                throw new NotFoundException($"Milestone ID {request.MilestoneID} nahi mila");

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Milestone Deleted: MilestoneID {milestone.MilestoneID}"));

            _context.CaseMilestones.Remove(milestone);
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
