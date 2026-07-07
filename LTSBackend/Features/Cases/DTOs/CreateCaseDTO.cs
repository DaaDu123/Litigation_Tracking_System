using System.ComponentModel.DataAnnotations;

namespace LTSBackend.Features.Cases.DTOs;

public class CreateCaseDTO
{
    [Required(ErrorMessage = "Case Number zaroori hai")]
    [StringLength(100)]
    public string CaseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Case Title zaroori hai")]
    [StringLength(255)]
    public string CaseTitle { get; set; } = string.Empty;

    public string? CaseDescription { get; set; }

    [Required(ErrorMessage = "Court zaroori hai")]
    public int CourtID { get; set; }

    [Required(ErrorMessage = "Category zaroori hai")]
    public int CategoryID { get; set; }

    [Required(ErrorMessage = "Priority zaroori hai")]
    [RegularExpression("^(High|Medium|Low)$")]
    public string Priority { get; set; } = "Medium";

    [Required(ErrorMessage = "Subject Matter zaroori hai")]
    [StringLength(255)]
    public string SubjectMatter { get; set; } = string.Empty;

    [Required(ErrorMessage = "Filing Date zaroori hai")]
    public DateTime FilingDate { get; set; }

    [Required(ErrorMessage = "Institution Date zaroori hai")]
    public DateTime InstitutionDate { get; set; }

    [Required(ErrorMessage = "Registration Date zaroori hai")]
    public DateTime RegistrationDate { get; set; }

    public DateTime? ExpectedDisposalDate { get; set; }

    public decimal ClaimedAmount { get; set; } = 0;

    public decimal PotentialLiability { get; set; } = 0;

    public string? FinancialImplication { get; set; }

    [Required(ErrorMessage = "Department zaroori hai")]
    public int ResponsibleDepartmentID { get; set; }

    [Required(ErrorMessage = "Legal Officer zaroori hai")]
    public int CurrentLegalOfficerID { get; set; }
}
