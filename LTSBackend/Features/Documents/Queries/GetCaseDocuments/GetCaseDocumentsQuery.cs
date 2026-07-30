using LTSBackend.Features.Documents.DTOs;
using MediatR;

namespace LTSBackend.Features.Documents.Queries.GetCaseDocuments
{
    /// <summary>
    /// SRS Reference: Litigation_Tracking_System_Case_SRS.docx Section 4 "Document
    /// Management" - "Centralized document repository". Backing query for the
    /// previously-missing "list documents for a case" endpoint.
    /// </summary>
    public record GetCaseDocumentsQuery(long CaseID) : IRequest<List<DocumentDetailDTO>>
    {
        public int UserID { get; init; }
    }
}
