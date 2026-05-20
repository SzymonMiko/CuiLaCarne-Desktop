using QuiLaCarne.Models;
using QuiLaCarne.Models.Base;

namespace QuiLaCarne.Models.Lookup;

public class Allergens : BaseNamedEntity
{
    public ICollection<Ingredients> Ingredients { get; set; } = new List<Ingredients>();
}