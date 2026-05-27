using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;

namespace QuiLaCarne.Models;

[Table("ingredients")]
public class Ingredients : BaseNamedEntity
{
    [NotMapped]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) ? Token : Name;

    public ICollection<Dishes> Dishes { get; set; } = new List<Dishes>();
    public ICollection<Models.Lookup.Allergens> Allergens { get; set; } = new List<Models.Lookup.Allergens>();
}
