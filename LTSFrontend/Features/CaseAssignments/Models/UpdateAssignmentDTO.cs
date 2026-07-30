namespace LTSFrontend.Features.CaseAssignments.Models
{
    public class UpdateAssignmentDTO
    {
        public long AssignmentID { get; set; }
        public string AssignmentType { get; set; } = string.Empty;
        public bool LeadCounsel { get; set; }
        public string? Remarks { get; set; }
    }
}
