using MediatR;
namespace LTSBackend.Features.Documents.Commands.UploadDocument
{
    public record UploadDocumentCommand(long CaseID,int DocumentTypeID,string DocumentName,IFormFile File,string? Remarks) : IRequest<UploadDocumentResult>
    {
        public int UserID { get; init; }
    }

    /// <summary>
    /// Result returned by UploadDocumentHandler.
    /// Carries the created DocumentID plus whether this was a
    /// Moharrir "blind upload" (restricted - uploader can't view/download it).
    /// </summary>
    public record UploadDocumentResult(long DocumentID, bool IsRestrictedMohallirUpload);
}