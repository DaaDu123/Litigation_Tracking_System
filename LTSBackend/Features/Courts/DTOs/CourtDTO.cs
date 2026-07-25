namespace LTSBackend.Features.Courts.DTOs;

public class CourtDTO
{
    public int CourtID { get; set; }

    public string CourtName { get; set; } = string.Empty;

    public string CourtType { get; set; } = string.Empty;

    public string Jurisdiction { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
}
