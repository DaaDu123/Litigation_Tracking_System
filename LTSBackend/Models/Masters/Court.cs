using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Masters;

[Table("Courts")]
public class Court
{
    [Key]
    public int CourtID { get; set; }

    [Required]
    [MaxLength(150)]
    public string CourtName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CourtType { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Jurisdiction { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;
}