namespace LTSFrontend.Core.DTOs
{
    /// <summary>
    /// Mirrors LTSBackend.Comman.Responses.PagedResult&lt;T&gt; so paged list
    /// endpoints (Cases, Hearings, Deadlines, etc.) deserialize correctly.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }
}
