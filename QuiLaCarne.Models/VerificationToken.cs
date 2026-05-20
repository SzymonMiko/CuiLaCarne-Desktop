using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Enums;

namespace QuiLaCarne.Models;

[Table("verification_tokens")]
public class VerificationToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("token_type")]
    public TokenTypeEnum TokenType { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    public Users User { get; set; } = null!;

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
}