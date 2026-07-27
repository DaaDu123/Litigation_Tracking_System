using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.DocumentPermissions;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Documents.Commands.DownloadDocument;

public class DownloadDocumentHandler(AppDbContext _context, IDocumentPermissionService _permissionService, IFileService _fileService,
    ILogger<DownloadDocumentHandler> _logger) : IRequestHandler<DownloadDocumentCommand, DocumentDownloadDTO>
{
    public async Task<DocumentDownloadDTO> Handle(DownloadDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Document download request - ID: {DocumentId}, User: {UserId}",request.DocumentID,request.UserID);

        // ================================================
        // 1. Check download permission
        // ================================================
        bool canDownload = await _permissionService.CanUserAccessDocumentAsync(request.UserID, request.DocumentID, "Download", cancellationToken);

        if (!canDownload)
        {
            _logger.LogWarning("User {UserId} denied download access to document {DocumentId}", request.UserID, request.DocumentID);
            throw new UnauthorizedException("You don't have permission to download this document. " +
                "If you are a restricted Moharrir, contact your administrator to grant access.");
        }

        // ================================================
        // 2. Fetch document
        // ================================================
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.DocumentID == request.DocumentID, cancellationToken);

        if (document == null)
        {
            _logger.LogWarning("Document not found: {DocumentId}", request.DocumentID);
            throw new NotFoundException($"Document {request.DocumentID} not found");
        }

        // ================================================
        // 3. Check if file exists on disk
        //    SECURITY FIX: documents are now stored via
        //    IFileService.SaveSecureFileAsync, outside wwwroot (see
        //    UploadDocumentHandler) - reading was still pointed at
        //    wwwroot here, which (a) would have broken every download
        //    once uploads moved to secure storage, and (b) was itself
        //    the other half of the vulnerability: reading straight from
        //    wwwroot only works at all because wwwroot is publicly
        //    servable by app.UseStaticFiles() with no authentication, so
        //    the exact same bytes returned by THIS permission-checked
        //    endpoint were also downloadable directly, bypassing every
        //    check above. IFileService.ReadSecureFileAsync resolves the
        //    path safely and throws FileNotFoundException if missing.
        // ================================================
        byte[] fileBytes;
        try
        {
            fileBytes = await _fileService.ReadSecureFileAsync(document.FilePath);
            _logger.LogInformation("Document file read successfully: {DocumentId}, Size: {Size} bytes",
                request.DocumentID,
                fileBytes.Length);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found on disk for document {DocumentId}: {FilePath}", request.DocumentID, document.FilePath);
            throw new InvalidOperationException("Document file not found on server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read document file: {FilePath}", document.FilePath);
            throw new InvalidOperationException("Failed to read document file");
        }

        // ================================================
        // 5. Return download DTO
        // ================================================
        var downloadDto = new DocumentDownloadDTO
        {
            DocumentID = document.DocumentID,
            FileName = document.FileName,
            FileBytes = fileBytes,
            ContentType = GetContentType(document.FileName),
            FileSize = fileBytes.Length
        };

        _logger.LogInformation("Document download prepared: {DocumentId}, FileName: {FileName}", request.DocumentID, document.FileName);
        return downloadDto;
    }
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}