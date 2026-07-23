using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Masters;

[Table("DocumentTypes")]
public class DocumentType
{
    [Key]
    public int DocumentTypeID { get; set; }

    [Required]
    [MaxLength(160)]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}