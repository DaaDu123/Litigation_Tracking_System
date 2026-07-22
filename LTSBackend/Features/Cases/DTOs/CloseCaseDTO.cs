namespace LTSBackend.Features.Cases.DTOs;

public class CloseCaseDTO
{
    public string ClosureRemarks { get; set; } = string.Empty;
    public string? JudgmentSummary { get; set; }
}