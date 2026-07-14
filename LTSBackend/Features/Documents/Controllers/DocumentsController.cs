using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Documents.Commands.DeleteDocument;
using LTSBackend.Features.Documents.Commands.DownloadDocument;
using LTSBackend.Features.Documents.Commands.UploadDocument;
using LTSBackend.Features.Documents.DTOs;
using LTSBackend.Features.Documents.Queries.GetDocument;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LTSBackend.Features.Documents.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentsController(IMediator _mediator, ILogger<DocumentsController> _logger) : ControllerBase
{
    // =====================================================
    // UPLOAD DOCUMENT
    // =====================================================
    /// <summary>
    /// Upload document to a case
    /// 
    /// MOHARRIR BLIND UPLOAD FEATURE:
    /// - Restricted Moharrir: Can upload but CANNOT view/download after upload
    /// - Elevated Moharrir: Can upload AND view/download
    /// 
    /// Role-based: Partner, Associate, Moharrir, InternParalegal can upload
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = RoleNames.CanViewDocuments + "," + RoleNames.InternParalegal)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentRequest request)
    {
        _logger.LogInformation("Upload document request - Case: {CaseId}, Type: {TypeId}, File: {FileName}",
            request.CaseID,
            request.DocumentTypeID,
            request.File?.FileName);

        // ================================================
        // Validate file
        // ================================================
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("File is required"));
        }

        if (request.File.Length > 50 * 1024 * 1024) // 50MB max
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("File size cannot exceed 50MB"));
        }

        // ================================================
        // Get current user ID
        // ================================================
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity"));
        }

        // ================================================
        // Create upload command
        // ================================================
        var command = new UploadDocumentCommand(request.CaseID, request.DocumentTypeID, request.DocumentName, request.File, request.Remarks)
        {
            UserID = userId
        };

        // ================================================
        // Execute upload
        // ================================================
        try
        {
            var result = await _mediator.Send(command);

            _logger.LogInformation("Document uploaded successfully - ID: {DocumentId}, Restricted: {Restricted}",
                result.DocumentID, result.IsRestrictedMohallirUpload);

            var response = new UploadDocumentResponseDTO
            {
                DocumentID = result.DocumentID,
                CaseID = request.CaseID,
                DocumentName = request.DocumentName,
                IsRestrictedMohallirUpload = result.IsRestrictedMohallirUpload,
                Message = result.IsRestrictedMohallirUpload
                    ? "Document uploaded successfully (restricted - you cannot view/download this file)"
                    : "Document uploaded successfully"
            };

            return Ok(ApiResponse<UploadDocumentResponseDTO>.SuccessResponse(
                response,
                response.Message));
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Upload unauthorized for user {UserId}", userId);
            return Forbid();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Upload failed - resource not found");
            return NotFound(ApiResponse<bool>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed");
            return BadRequest(ApiResponse<bool>.FailureResponse($"Upload failed: {ex.Message}"));
        }
    }

    // =====================================================
    // DOWNLOAD DOCUMENT
    // =====================================================
    /// <summary>
    /// Download document
    /// 
    /// MOHARRIR RESTRICTED FEATURE:
    /// Moharrir restricted mode: CANNOT download
    /// Elevated Moharrir: Can download
    /// Other roles: Can download based on permissions
    /// </summary>
    [HttpGet("download/{documentId}")]
    public async Task<IActionResult> DownloadDocument(long documentId)
    {
        _logger.LogInformation("Download document request - ID: {DocumentId}", documentId);

        // ================================================
        // Get current user ID
        // ================================================
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity"));
        }

        // ================================================
        // Execute download
        // ================================================
        try
        {
            var command = new DownloadDocumentCommand(documentId) { UserID = userId };
            var downloadData = await _mediator.Send(command);

            // Return file for download
            return File(downloadData.FileBytes,downloadData.ContentType,downloadData.FileName);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Download unauthorized for user {UserId} document {DocumentId}", userId, documentId);
            return Forbid();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Download failed - document not found {DocumentId}", documentId);
            return NotFound(ApiResponse<bool>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document download failed");
            return BadRequest(ApiResponse<bool>.FailureResponse($"Download failed: {ex.Message}"));
        }
    }

    // =====================================================
    // GET DOCUMENT DETAILS
    // =====================================================
    /// <summary>
    /// Get document metadata
    /// 
    /// MOHARRIR RESTRICTED FEATURE:
    /// Moharrir restricted mode: CANNOT view
    /// Elevated Moharrir: Can view
    /// Other roles: Can view based on permissions
    /// </summary>
    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDocument(long documentId)
    {
        _logger.LogInformation("Get document request - ID: {DocumentId}", documentId);

        // ================================================
        // Get current user ID
        // ================================================
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity"));
        }

        // ================================================
        // Execute query
        // ================================================
        try
        {
            var query = new GetDocumentQuery(documentId) { UserID = userId };
            var document = await _mediator.Send(query);

            if (document == null)
            {
                return NotFound(ApiResponse<bool>.FailureResponse("Document not found"));
            }

            return Ok(ApiResponse<DocumentDetailDTO>.SuccessResponse(document,"Document retrieved successfully"));
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "View unauthorized for user {UserId} document {DocumentId}", userId, documentId);
            return Forbid();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Get document failed - not found {DocumentId}", documentId);
            return NotFound(ApiResponse<bool>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document");
            return BadRequest(ApiResponse<bool>.FailureResponse($"Failed to retrieve document: {ex.Message}"));
        }
    }

    // =====================================================
    // DELETE DOCUMENT
    // =====================================================
    /// <summary>
    /// Delete document (hard delete)
    /// Role-based: Partner and FirmAdmin only
    /// 
    /// Also deletes associated file from disk
    /// and removes all document permissions
    /// </summary>
    [HttpDelete("{documentId}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> DeleteDocument(long documentId)
    {
        _logger.LogInformation("Delete document request - ID: {DocumentId}", documentId);

        // ================================================
        // Get current user ID
        // ================================================
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid user identity"));
        }

        // ================================================
        // Execute delete
        // ================================================
        try
        {
            var command = new DeleteDocumentCommand(documentId) { UserID = userId };
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.SuccessResponse(result,"Document deleted successfully"));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Delete failed - document not found {DocumentId}", documentId);
            return NotFound(ApiResponse<bool>.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document deletion failed");
            return BadRequest(ApiResponse<bool>.FailureResponse($"Deletion failed: {ex.Message}"));
        }
    }
}