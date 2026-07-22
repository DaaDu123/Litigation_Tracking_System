using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Milestones.Commands.UpdateMilestone
{
    public class UpdateMilestoneHandler(AppDbContext _context, IAuditService _auditService,
        ICurrentUserService _currentUser, IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateMilestoneCommand, bool>
    {
        public async Task<bool> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await _context.CaseMilestones
                .Include(m => m.Case)
                .FirstOrDefaultAsync(m => m.MilestoneID == request.Milestone.MilestoneID, cancellationToken);

            if (milestone == null || (!_currentUser.IsSuperAdmin && milestone.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Milestone ID {request.Milestone.MilestoneID} nahi mila");

            milestone.Milestone = request.Milestone.Milestone;
            milestone.MilestoneDate = request.Milestone.MilestoneDate;
            milestone.Description = request.Milestone.Description;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Milestone Updated: MilestoneID {milestone.MilestoneID}"));

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