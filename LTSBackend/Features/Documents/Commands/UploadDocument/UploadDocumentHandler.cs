using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using LTSBackend.Services.DocumentPermissions;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Documents.Commands.UploadDocument;

/// <summary>
/// Upload document handler with Moharrir blind upload (write-only) feature
/// </summary>
public class UploadDocumentHandler (AppDbContext _context, IFileService _fileService, IDocumentPermissionService _permissionService, IAuditService _auditService,
    IHttpContextAccessor _httpContextAccessor, ILogger<UploadDocumentHandler> _logger) : IRequestHandler<UploadDocumentCommand, long>
{   
    public async Task<long> Handle(UploadDocumentCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Document upload started for case {CaseId} by user {UserId}",request.CaseID,request.UserID);

        // ================================================
        // 1. Get current user
        // ================================================
        var user = await _context.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Upload failed: User not found {UserId}", request.UserID);
            throw new NotFoundException($"User {request.UserID} not found");
        }

        // ================================================
        // 2. Check upload permission
        // ================================================
        bool canUpload = await _permissionService.CanUserAccessDocumentAsync(request.UserID,0, "Upload",cancellationToken);
        if (!canUpload)
        {
            _logger.LogWarning("Upload denied: User {UserId} cannot upload documents", request.UserID);
            throw new UnauthorizedException("You don't have permission to upload documents");
        }

        // ================================================
        // 3. Verify case exists
        // ================================================
        var caseRecord = await _context.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CaseID == request.CaseID, cancellationToken);

        if (caseRecord == null)
        {
            _logger.LogWarning("Upload failed: Case not found {CaseId}", request.CaseID);
            throw new NotFoundException($"Case {request.CaseID} not found");
        }

        // ================================================
        // 4. Verify document type exists
        // ================================================
        var documentType = await _context.DocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentTypeID == request.DocumentTypeID, cancellationToken);

        if (documentType == null)
        {
            _logger.LogWarning("Upload failed: Document type not found {TypeId}", request.DocumentTypeID);
            throw new NotFoundException($"Document type {request.DocumentTypeID} not found");
        }

        // ================================================
        // 5. Save file to disk
        // ================================================
        string filePath;
        try
        {
            filePath = await _fileService.SaveFileAsync(request.File,"case_documents");

            _logger.LogInformation("File saved to disk: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file for case {CaseId}", request.CaseID);
            throw new InvalidOperationException("Failed to save document file");
        }

        // ================================================
        // 6. Create document record
        // ================================================
        var document = new Document
        {
            CaseID = request.CaseID,
            DocumentTypeID = request.DocumentTypeID,
            DocumentName = request.DocumentName,
            FileName = request.File.FileName,
            FilePath = filePath,
            FileSize = request.File.Length,
            VersionNo = 1,
            UploadedBy = request.UserID,
            UploadedDate = DateTime.UtcNow,
            IsLatest = true,
            Remarks = request.Remarks
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document created with ID {DocumentId} for case {CaseId}",document.DocumentID,request.CaseID);

        // ================================================
        // 7. CRITICAL: Handle Moharrir blind upload feature
        //    Agar Moharrir restricted mode mein hai to NO DocumentPermission entry
        //    "Blind upload" = write-only, can't view/download after upload
        // ================================================
        bool isMohallirRestricted = await _permissionService.IsMohallirRestrictedAsync(request.UserID,cancellationToken);
        if (isMohallirRestricted)
        {
            // ================================================
            // Restricted Moharrir: Do NOT grant view/download permissions
            // The document exists but they can't see it after uploading
            // ================================================
            _logger.LogInformation("Moharrir {UserId} blind upload: Document {DocumentId} - no view/download permissions granted",
                request.UserID,document.DocumentID);

            // IMPORTANT: No DocumentPermission entry = write-only
        }
        else
        {
            // ================================================
            // Elevated Moharrir or other roles: Grant appropriate permissions
            // ================================================
            var role = user.Role;
            if (role != null)
            {
                // Determine permissions based on role
                bool canView = true;
                bool canDownload = user.GetRole() switch
                {
                    UserRole.Partner => true,
                    UserRole.AssociateLawyer => true,
                    UserRole.Moharrir => true, // Elevated only
                    UserRole.InternParalegal => false,
                    _ => false
                };

                await _permissionService.GrantDocumentPermissionAsync(document.DocumentID,role.RoleID,canView,canDownload,true,cancellationToken);

                _logger.LogInformation("Document permissions granted for role {RoleId}: View={CanView}, Download={CanDownload}",
                    role.RoleID,canView,canDownload);
            }
        }

        // ================================================
        // 8. Create audit log
        // ================================================
        var auditLog = _auditService.Create(request.UserID,$"Document Upload: {document.DocumentName} to Case {request.CaseID}");

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document upload completed - ID: {DocumentId}, User: {UserId}, Case: {CaseId}",
            document.DocumentID,request.UserID,request.CaseID);

        return document.DocumentID;
    }
}