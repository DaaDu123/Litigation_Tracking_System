namespace LTSFrontend.Features.Masters.DTOs
{
    /// <summary>Mirrors LTSBackend.Features.DocumentTypes.DTOs.DocumentTypeDTO</summary>
    public class DocumentTypeDTO
    {
        public int DocumentTypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
