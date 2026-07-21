using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Milestones.Commands.CreateMilestone
{
    public class CreateMilestoneHandler(AppDbContext _context,IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<CreateMilestoneCommand, long>
    {
        public async Task<long> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
        {
            var caseExists = await _context.Cases.AnyAsync(c => c.CaseID == request.Milestone.CaseID, cancellationToken);
            if (!caseExists)
                throw new NotFoundException($"Case ID {request.Milestone.CaseID} nahi mila");

            var milestone = new CaseMilestone
            {
                CaseID = request.Milestone.CaseID,
                Milestone = request.Milestone.Milestone,
                MilestoneDate = request.Milestone.MilestoneDate,
                Description = request.Milestone.Description
            };

            _context.CaseMilestones.Add(milestone);

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Milestone Created: {milestone.Milestone} for Case {request.Milestone.CaseID}"));

            await _context.SaveChangesAsync(cancellationToken);
            return milestone.MilestoneID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
