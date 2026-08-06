namespace LTSFrontend.Features.Documents.DTOs
{
    /// <summary>
    /// Client-side representation of a downloaded document's bytes, mirroring
    /// LTSBackend.Features.Documents.Commands.DownloadDocument.DocumentDownloadDTO.
    /// The backend's /api/documents/download/{id} endpoint returns the raw file
    /// (not JSON) - DocumentService.DownloadAsync reads the response stream
    /// directly and hands the bytes to the browser via JS interop, so this type
    /// isn't required for that flow. It's kept as a typed shape for any future
    /// caller that wants to work with a downloaded document in memory (e.g. a
    /// preview pane) without re-parsing the raw HttpResponseMessage itself.
    /// </summary>
    public class DocumentDownloadDTO
    {
        public long DocumentID { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public long FileSize { get; set; }
    }
}
