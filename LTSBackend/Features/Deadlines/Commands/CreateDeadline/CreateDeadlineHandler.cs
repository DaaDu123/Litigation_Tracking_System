using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Deadlines.Commands.CreateDeadline
{
    public class CreateDeadlineHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor,
        ILogger<CreateDeadlineHandler> _logger) : IRequestHandler<CreateDeadlineCommand, long>
    {
        public async Task<long> Handle(CreateDeadlineCommand request, CancellationToken cancellationToken)
        {
            var caseExists = await _context.Cases.AnyAsync(c => c.CaseID == request.Deadline.CaseID, cancellationToken);
            if (!caseExists)
                throw new NotFoundException($"Case ID {request.Deadline.CaseID} nahi mila");

            var deadline = new Deadline
            {
                CaseID = request.Deadline.CaseID,
                DeadlineType = request.Deadline.DeadlineType,
                DueDate = request.Deadline.DueDate,
                ReminderDays = request.Deadline.ReminderDays,
                Remarks = request.Deadline.Remarks,
                Completed = false
            };

            _context.Deadlines.Add(deadline);

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId,
                $"Deadline Created: {deadline.DeadlineType} due {deadline.DueDate:d} for Case {request.Deadline.CaseID}"));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deadline created: {DeadlineID} for Case {CaseID}", deadline.DeadlineID, request.Deadline.CaseID);

            return deadline.DeadlineID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
