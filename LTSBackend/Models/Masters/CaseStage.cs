using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Masters;

[Table("CaseStages")]
public class CaseStage
{
    [Key]
    public int StageID { get; set; }

    [Required]
    [MaxLength(150)]
    public string StageName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}