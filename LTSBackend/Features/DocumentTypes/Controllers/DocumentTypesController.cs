using LTSBackend.Comman.Responses;
using LTSBackend.Features.DocumentTypes.Commands.CreateDocumentType;
using LTSBackend.Features.DocumentTypes.Commands.DeleteDocumentType;
using LTSBackend.Features.DocumentTypes.Commands.UpdateDocumentType;
using LTSBackend.Features.DocumentTypes.DTOs;
using LTSBackend.Features.DocumentTypes.Queries.GetAllDocumentTypes;
using LTSBackend.Features.DocumentTypes.Queries.GetDocumentTypeById;
using LTSBackend.Models.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTSBackend.Features.DocumentTypes.Controllers;

/// <summary>
/// Master data for document types (e.g. Petition, Affidavit, Court Order).
/// Same per-tenant model as Courts/Departments/CaseCategories/CaseStages -
/// see CreateDocumentTypeHandler/UpdateDocumentTypeHandler/
/// DeleteDocumentTypeHandler for the ownership rules.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentTypesController(IMediator mediator) : ControllerBase
{
    // =====================================================
    // GET ALL DOCUMENT TYPES — Any authenticated user
    // Returns the document types (Petition, Affidavit, Court Order,
    // Evidence, Reply, Judgment, etc.) available to the caller's firm, for
    // Document upload/edit dropdowns.
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchText, [FromQuery] bool activeOnly = true)
    {
        var types = await mediator.Send(new GetAllDocumentTypesQuery(searchText, activeOnly));
        return Ok(ApiResponse<List<DocumentTypeDTO>>.SuccessResponse(types));
    }

    // =====================================================
    // GET DOCUMENT TYPE BY ID — Any authenticated user
    // Fetches a single document type's details by its ID.
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var type = await mediator.Send(new GetDocumentTypeByIdQuery(id));
        return Ok(ApiResponse<DocumentTypeDTO>.SuccessResponse(type));
    }

    // =====================================================
    // CREATE DOCUMENT TYPE — FirmAdmin and above
    // Adds a new document type. A FirmAdmin's new type is scoped to their
    // own firm; only a SuperAdmin can create a global type.
    // =====================================================
    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create(CreateDocumentTypeCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Document type created successfully."));
    }

    // =====================================================
    // UPDATE DOCUMENT TYPE — FirmAdmin and above
    // Edits an existing document type's name/description/active flag.
    // Route id and body DocumentTypeID must match.
    // =====================================================
    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, UpdateDocumentTypeCommand command)
    {
        if (id != command.DocumentTypeID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body DocumentTypeID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Document type updated successfully."));
    }

    // =====================================================
    // DELETE DOCUMENT TYPE — FirmAdmin and above
    // Removes a document type the firm no longer uses. Ownership/in-use
    // checks are enforced in the handler.
    // =====================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteDocumentTypeCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Document type deleted successfully."));
    }
}
