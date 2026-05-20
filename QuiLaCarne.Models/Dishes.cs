using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;

namespace QuiLaCarne.Models;

[Table("dishes")]
public class Dishes : BaseNamedEntity
{
    [Column("description", TypeName = "TEXT")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("available_from")]
    public DateTimeOffset? AvailableFrom { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    public Models.Lookup.DishesCategories Category { get; set; } = null!;

    public ICollection<Ingredients> Ingredients { get; set; } = new List<Ingredients>();
}