using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuiLaCarne.Models.Base;

public abstract class BaseNamedEntity : BaseEntity
{
    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
