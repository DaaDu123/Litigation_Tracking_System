using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Cases.Commands.UpdateCaseStatus;

public class UpdateCaseStatusHandler (AppDbContext _context, IAuditService _auditService, ILogger<UpdateCaseStatusHandler> _logger, IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateCaseStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateCaseStatusCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case status update kia ja raha hai: {CaseID}", request.CaseID);

        int currentUserId = GetCurrentUserId();

        // ================================================
        // 1. Find Case
        // ================================================
        var caseToUpdate = await _context.Cases
            .FirstOrDefaultAsync(x => x.CaseID == request.CaseID, cancellationToken);

        if (caseToUpdate == null)
        {
            _logger.LogWarning("Case nahi mila: {CaseID}", request.CaseID);
            throw new NotFoundException($"Case ID {request.CaseID} nahi mila");
        }

        // ================================================
        // 2. Verify new status exist karta hai
        // ================================================
        var newStatus = await _context.CaseStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StatusID == request.NewStatusID, cancellationToken);

        if (newStatus == null)
        {
            _logger.LogWarning("Status nahi mila: {StatusID}", request.NewStatusID);
            throw new NotFoundException($"Status ID {request.NewStatusID} nahi mila");
        }

        // ================================================
        // 3. Check agar same status hai to skip kare
        // ================================================
        if (caseToUpdate.StatusID == request.NewStatusID)
        {
            _logger.LogWarning("Same status set kara ja raha hai: {CaseID}", request.CaseID);
            throw new ValidationException(new List<string>
            {
                "Naya status pehle wala status jaisa hai"
            });
        }

        // ================================================
        // 4. Store old status
        // ================================================
        int oldStatusID = caseToUpdate.StatusID;

        // ================================================
        // 5. Update case status
        //    FIX: sync IsClosed / ClosureDate with the new status.
        //    Previously the Case.IsClosed flag (used by vw_ActiveCases /
        //    vw_ClosedCases) was never updated here, so a case could
        //    move to a "Closed" status yet still show up as active.
        // ================================================
        caseToUpdate.StatusID = request.NewStatusID;
        caseToUpdate.IsClosed = newStatus.IsClosed;

        if (newStatus.IsClosed)
        {
            // Only stamp ClosureDate the first time it becomes closed
            caseToUpdate.ClosureDate ??= DateTime.UtcNow.Date;
        }
        else
        {
            // Case re-opened (moved to a non-closed status) - clear closure date
            caseToUpdate.ClosureDate = null;
        }

        caseToUpdate.ModifiedBy = currentUserId;
        caseToUpdate.ModifiedDate = DateTime.UtcNow;

        // ================================================
        // 6. Create status history entry
        // ================================================
        var statusHistory = new CaseStatusHistory
        {
            CaseID = request.CaseID,
            OldStatusID = oldStatusID,
            NewStatusID = request.NewStatusID,
            ChangedBy = currentUserId,
            ChangedDate = DateTime.UtcNow,
            Remarks = request.Remarks
        };

        _context.CaseStatusHistories.Add(statusHistory);

        // ================================================
        // 7. Create Audit Log
        // ================================================
        var auditLog = _auditService.Create(
            currentUserId,
            $"Case Status Update: {caseToUpdate.CaseNumber} - {newStatus.StatusName}");

        _context.AuditLogs.Add(auditLog);

        // ================================================
        // 8. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Case status successfully updated: {CaseID} from {OldStatus} to {NewStatus}",
            request.CaseID, oldStatusID, request.NewStatusID);

        return true;
    }

    /// <summary>
    /// Get current logged-in user ID from HttpContext
    /// </summary>
    private int GetCurrentUserId()
    {
        int currentUserId = 1; // Default fallback

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var userIdClaim = httpContext.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) &&
                    int.TryParse(userIdClaim, out var userId))
                {
                    currentUserId = userId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current user ID from context");
        }

        return currentUserId;
    }
}