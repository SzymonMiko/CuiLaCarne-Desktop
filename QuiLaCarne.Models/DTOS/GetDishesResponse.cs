using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class GetDishesResponse
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public DateTimeOffset? AvailableFrom { get; set; }

    public string CategoryToken { get; set; } = "";

    public List<string> IngredientTokens { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
