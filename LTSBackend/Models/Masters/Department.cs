using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTSBackend.Models.Masters;

[Table("Departments")]
public class Department
{
    [Key]
    public int DepartmentID { get; set; }

    [Required]
    [MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}