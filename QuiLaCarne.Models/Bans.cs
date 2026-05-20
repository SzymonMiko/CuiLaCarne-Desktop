using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("bans")]
public class Bans : BaseEntity
{
    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("is_permanent")]
    public bool? IsPermanent { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    public Users User { get; set; } = null!;

    public ICollection<BanStatus> Statuses { get; set; } = new List<BanStatus>();
}