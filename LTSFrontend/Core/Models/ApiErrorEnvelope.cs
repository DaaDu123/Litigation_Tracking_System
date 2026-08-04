namespace LTSFrontend.Core.Models
{
    /// <summary>
    /// Shape used ONLY for parsing a failed (non-2xx) response body. Unlike
    /// ApiResponse&lt;T&gt;, this has no Data property, so it can always be
    /// deserialized regardless of what T the calling ApiClient method
    /// expects back on success - see ApiClient.SendAsync for why that
    /// distinction matters.
    /// </summary>
    public class ApiErrorEnvelope
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
