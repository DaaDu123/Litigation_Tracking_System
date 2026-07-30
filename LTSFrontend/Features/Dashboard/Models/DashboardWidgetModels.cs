namespace LTSFrontend.Features.Dashboard.Models
{
    /// <summary>One bar in a <c>ChartWidget</c> (e.g. a case status or priority bucket).</summary>
    public class ChartItem
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        /// <summary>CSS color for the bar, e.g. "#1b4fd6". Falls back to the widget's default accent when null.</summary>
        public string? Color { get; set; }
    }

    /// <summary>One row in a <c>RecentActivityPanel</c> (an audit log entry, a recently touched case, etc.).</summary>
    public class ActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public DateTime? Timestamp { get; set; }
        /// <summary>Bootstrap Icons class, e.g. "bi bi-briefcase". Defaults to a generic dot if omitted.</summary>
        public string? Icon { get; set; }
        /// <summary>Optional click-through link (case details, user profile, etc.).</summary>
        public string? Href { get; set; }
    }
}
