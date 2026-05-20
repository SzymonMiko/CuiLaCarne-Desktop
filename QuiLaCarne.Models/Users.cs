using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("users")]
public class Users : BaseEntity
{
    [Required]
    [MaxLength(255)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = false;

    [Column("banned_until")]
    public DateTimeOffset? BannedUntil { get; set; }

    public ICollection<Roles> Roles { get; set; } = new List<Roles>();
    public ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();
    public ICollection<Bans> Bans { get; set; } = new List<Bans>();
    public ICollection<GuestReports> GuestReports { get; set; } = new List<GuestReports>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}