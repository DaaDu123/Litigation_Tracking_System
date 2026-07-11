namespace LTSBackend.Features.Documents.Commands.DeleteDocument
{
    public class DeleteDocumentResponseDTO
    {
        public long DocumentID { get; set; }
        public bool IsDeleted { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
