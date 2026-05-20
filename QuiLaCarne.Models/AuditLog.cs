using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;

namespace QuiLaCarne.Models;

[Table("audit_logs")]
public class AuditLog : BaseEntity
{
    [Required]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("details", TypeName = "TEXT")]
    public string? Details { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    public Users User { get; set; } = null!;
}