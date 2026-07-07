namespace LTSBackend.Features.Cases.DTOs;
public class CaseDTO
{
    public long CaseID { get; set; }
    public string InternalReferenceNo { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string? CaseDescription { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string LegalOfficerName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string SubjectMatter { get; set; } = string.Empty;
    public DateTime FilingDate { get; set; }
    public DateTime InstitutionDate { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime? ExpectedDisposalDate { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal PotentialLiability { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedDate { get; set; }
}