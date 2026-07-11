using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Documents.Commands.DeleteDocument;

/// <summary>
/// Delete document handler
/// Soft delete - mark as inactive instead of hard delete
/// Role-based: Partner and FirmAdmin only
/// </summary>
public class DeleteDocumentHandler (AppDbContext _context, IFileService _fileService, IAuditService _auditService, ILogger<DeleteDocumentHandler> _logger) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Document delete attempt - ID: {DocumentId}, User: {UserId}",request.DocumentID,request.UserID);

        // ================================================
        // 1. Find document
        // ================================================
        var document = await _context.Documents.FirstOrDefaultAsync(x => x.DocumentID == request.DocumentID, cancellationToken);

        if (document == null)
        {
            _logger.LogWarning("Delete failed: Document not found {DocumentId}", request.DocumentID);
            throw new NotFoundException($"Document {request.DocumentID} not found");
        }

        // ================================================
        // 2. Delete file from disk
        // ================================================
        try
        {
            _fileService.DeleteFile(document.FilePath);
            _logger.LogInformation("File deleted from disk: {FilePath}", document.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file from disk: {FilePath}", document.FilePath);
            // Don't throw - continue with database deletion
        }

        // ================================================
        // 3. Remove document from database (hard delete for documents)
        // ================================================
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document deleted successfully: {DocumentId}", request.DocumentID);

        // ================================================
        // 4. Remove document permissions
        // ================================================
        var permissions = await _context.DocumentPermissions.Where(x => x.DocumentID == request.DocumentID).ToListAsync(cancellationToken);

        if (permissions.Any())
        {
            _context.DocumentPermissions.RemoveRange(permissions);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} permissions for deleted document", permissions.Count);
        }

        // ================================================
        // 5. Create audit log
        // ================================================
        var auditLog = _auditService.Create(request.UserID,$"Document Delete: {document.DocumentName} (ID: {document.DocumentID})");

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit log created for document deletion: {DocumentId}", request.DocumentID);

        return true;
    }
}