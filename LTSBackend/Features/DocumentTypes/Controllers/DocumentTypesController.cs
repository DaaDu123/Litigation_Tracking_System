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
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchText, [FromQuery] bool activeOnly = true)
    {
        var types = await mediator.Send(new GetAllDocumentTypesQuery(searchText, activeOnly));
        return Ok(ApiResponse<List<DocumentTypeDTO>>.SuccessResponse(types));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var type = await mediator.Send(new GetDocumentTypeByIdQuery(id));
        return Ok(ApiResponse<DocumentTypeDTO>.SuccessResponse(type));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Create(CreateDocumentTypeCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(ApiResponse<int>.SuccessResponse(id, "Document type created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Update(int id, UpdateDocumentTypeCommand command)
    {
        if (id != command.DocumentTypeID)
            return BadRequest(ApiResponse<bool>.FailureResponse("Route ID and body DocumentTypeID do not match."));

        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Document type updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.FirmAdminAndAbove)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await mediator.Send(new DeleteDocumentTypeCommand(id));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Document type deleted successfully."));
    }
}
