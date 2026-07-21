using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Models.Security;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseAssignments.Commands.AssignCase
{
    /// <summary>
    /// SRS Reference: Litigation_Tracking_System_Case_SRS.docx UC-04 "Assign Case to Counsel"
    /// FR-06 "System shall allow assignment of cases to legal officers and counsel"
    /// Also triggers a Notification (SRS: "System sends notification")
    /// </summary>
    public class AssignCaseHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor,
        ILogger<AssignCaseHandler> _logger) : IRequestHandler<AssignCaseCommand, long>
    {
        public async Task<long> Handle(AssignCaseCommand request, CancellationToken cancellationToken)
        {
            var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.CaseID == request.Assignment.CaseID, cancellationToken);
            if (caseEntity == null)
                throw new NotFoundException($"Case ID {request.Assignment.CaseID} nahi mila");

            var userExists = await _context.Users.AnyAsync(u => u.UserID == request.Assignment.UserID && u.IsActive, cancellationToken);
            if (!userExists)
                throw new NotFoundException($"User ID {request.Assignment.UserID} nahi mila ya inactive hai");

            // Prevent duplicate active assignment of the same user+type on the same case
            var duplicate = await _context.CaseAssignments.AnyAsync(a =>
                a.CaseID == request.Assignment.CaseID &&
                a.UserID == request.Assignment.UserID &&
                a.AssignmentType == request.Assignment.AssignmentType &&
                a.EndDate == null, cancellationToken);

            if (duplicate)
                throw new ValidationException(new List<string> { "Yeh user pehle se hi is type ke sath is case par active assign hai" });

            int currentUserId = GetCurrentUserId();

            var assignment = new CaseAssignment
            {
                CaseID = request.Assignment.CaseID,
                UserID = request.Assignment.UserID,
                AssignmentType = request.Assignment.AssignmentType,
                LeadCounsel = request.Assignment.LeadCounsel,
                AssignedBy = currentUserId,
                AssignedDate = DateTime.UtcNow,
                Remarks = request.Assignment.Remarks
            };

            _context.CaseAssignments.Add(assignment);

            // If lead counsel, sync Case.CurrentLegalOfficerID for quick lookups / dashboards
            if (request.Assignment.LeadCounsel)
            {
                caseEntity.CurrentLegalOfficerID = request.Assignment.UserID;
                caseEntity.ModifiedBy = currentUserId;
                caseEntity.ModifiedDate = DateTime.UtcNow;
            }

            // SRS: "System sends notification" (UC-04 postcondition)
            _context.Notifications.Add(new Notification
            {
                NotificationTypeID = 3, // CaseAssignment (seeded in AppDbContext)
                UserID = request.Assignment.UserID,
                CaseID = request.Assignment.CaseID,
                Subject = "Naya case assign hua hai",
                Message = $"Aapko case {caseEntity.CaseNumber} ({caseEntity.CaseTitle}) '{request.Assignment.AssignmentType}' ke tor par assign kiya gaya hai.",
                Priority = "Medium",
                CreatedDate = DateTime.UtcNow
            });

            _context.AuditLogs.Add(_auditService.Create(currentUserId,
                $"Case Assigned: UserID {request.Assignment.UserID} to Case {request.Assignment.CaseID} as {request.Assignment.AssignmentType}"));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Case {CaseID} assigned to User {UserID}", request.Assignment.CaseID, request.Assignment.UserID);

            return assignment.AssignmentID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
