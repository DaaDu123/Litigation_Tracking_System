using LTSBackend.Comman.Responses;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Documents.Commands.UploadDocument;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LTSBackend.Features.Documents.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentsController (IMediator _mediator, ILogger<DocumentsController> _logger) : ControllerBase
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
    [Authorize(Roles = RoleNames.CanViewDocuments + "," + RoleNames.InternParalegal)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] long caseId,
        [FromForm] int documentTypeId,
        [FromForm] string documentName,
        [FromForm] IFormFile file,
        [FromForm] string? remarks = null)
    {
        _logger.LogInformation("Upload document request - Case: {CaseId}, Type: {TypeId}, File: {FileName}",caseId,documentTypeId,file?.FileName);

        // ================================================
        // Validate file
        // ================================================
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("File is required"));
        }
        if (file.Length > 50 * 1024 * 1024) // 50MB max
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
        var command = new UploadDocumentCommand(caseId,documentTypeId,documentName,file,remarks)
        {
            UserID = userId
        };
        // ================================================
        // Execute upload
        // ================================================
        try
        {
            var documentId = await _mediator.Send(command);
            _logger.LogInformation("Document uploaded successfully - ID: {DocumentId}", documentId);
            var response = new UploadDocumentResponseDTO
            {
                DocumentID = documentId,
                CaseID = caseId,
                DocumentName = documentName,
                Message = "Document uploaded successfully"
            };
            return Ok(ApiResponse<UploadDocumentResponseDTO>.SuccessResponse(response,"Document uploaded successfully"));
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
    /// Moharrir restricted mode: CANNOT download
    /// Other roles: Can download based on permissions
    /// </summary>
    [HttpGet("download/{documentId}")]
    public async Task<IActionResult> DownloadDocument(long documentId)
    {
        _logger.LogInformation("Download document request - ID: {DocumentId}", documentId);
        return Ok(ApiResponse<bool>.SuccessResponse(true,"Implement download handler"));
    }
    // =====================================================
    // GET DOCUMENT DETAILS
    // =====================================================
    /// <summary>
    /// Get document metadata
    /// 
    /// Moharrir restricted mode: CANNOT view
    /// </summary>
    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDocument(long documentId)
    {
        _logger.LogInformation("Get document request - ID: {DocumentId}", documentId);
        return Ok(ApiResponse<bool>.SuccessResponse(true,"Implement document retrieval handler"));
    }
    // =====================================================
    // DELETE DOCUMENT
    // =====================================================
    /// <summary>
    /// Delete document (soft delete)
    /// Role-based: Partner and FirmAdmin only
    /// </summary>
    [HttpDelete("{documentId}")]
    [Authorize(Roles = RoleNames.PartnerAndAbove)]
    public async Task<IActionResult> DeleteDocument(long documentId)
    {
        _logger.LogInformation("Delete document request - ID: {DocumentId}", documentId);
        return Ok(ApiResponse<bool>.SuccessResponse(true,"Implement document deletion handler"));
    }
}