using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Documents.Commands.ApproveDocument;

/// <summary>
/// Approves a draft document (SRS Intern/Paralegal draft workflow).
/// Role check ([Authorize(Roles = RoleNames.PartnerAndAbove)] on the
/// controller) already restricts this to Partner/FirmAdmin, so this
/// handler focuses on: document must exist, must belong to the approver's
/// own firm (tenant isolation), and must actually still be a draft.
/// </summary>
public class ApproveDocumentHandler(AppDbContext _context, IAuditService _auditService, ICurrentUserService _currentUser, ILogger<ApproveDocumentHandler> _logger) : IRequestHandler<ApproveDocumentCommand, bool>
{
    public async Task<bool> Handle(ApproveDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approve document attempt - ID: {DocumentId}, User: {UserId}", request.DocumentID, request.UserID);

        // ================================================
        // 1. Load document + its case's FirmID (tenant isolation - a
        //    Partner/FirmAdmin from another firm must never be able to
        //    approve/see this document just by knowing the ID).
        // ================================================
        var document = await _context.Documents.Include(d => d.Case).FirstOrDefaultAsync(d => d.DocumentID == request.DocumentID, cancellationToken);

        if (document == null)
        {
            _logger.LogWarning("Approve failed: Document not found {DocumentId}", request.DocumentID);
            throw new NotFoundException($"Document {request.DocumentID} not found");
        }

        if (document.Case == null || document.Case.FirmID != _currentUser.FirmID)
        {
            _logger.LogWarning("Approve denied: cross-firm access blocked for document {DocumentId}", request.DocumentID);
            throw new NotFoundException($"Document {request.DocumentID} not found");
        }

        // ================================================
        // 2. Must actually be a pending draft.
        // ================================================
        if (!document.IsDraft)
        {
            _logger.LogWarning("Approve failed: Document {DocumentId} is not a pending draft", request.DocumentID);
            throw new ValidationException(["This document is not pending approval"]);
        }

        // ================================================
        // 3. Publish it.
        // ================================================
        document.IsDraft = false;
        document.ApprovedBy = request.UserID;
        document.ApprovedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // ================================================
        // 4. Audit log.
        // ================================================
        var auditLog = _auditService.Create(request.UserID, $"Document Approved: {document.DocumentName} (ID: {document.DocumentID})");
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} approved by user {UserId}", request.DocumentID, request.UserID);
        return true;
    }
}
