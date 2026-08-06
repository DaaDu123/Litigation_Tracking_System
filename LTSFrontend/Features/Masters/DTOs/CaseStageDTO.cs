namespace LTSFrontend.Features.Masters.DTOs
{
    /// <summary>Mirrors LTSBackend.Features.CaseStages.DTOs.CaseStageDTO</summary>
    public class CaseStageDTO
    {
        public int StageID { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
