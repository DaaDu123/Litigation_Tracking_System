using MediatR;

namespace LTSBackend.Features.Documents.Commands.ApproveDocument
{
    // Publishes a draft document (uploaded by an Intern/Paralegal) so it is
    // no longer restricted to the uploader + Partner/FirmAdmin. Only
    // Partner/FirmAdmin can call this (see [Authorize] on the controller
    // action) - matches the SRS: "All uploaded work remains in Draft until
    // approved by Partner or Firm Admin."
    public record ApproveDocumentCommand(long DocumentID) : IRequest<bool>
    {
        public int UserID { get; init; }
    }
}
