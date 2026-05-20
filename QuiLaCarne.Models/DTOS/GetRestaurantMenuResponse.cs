using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class DishMenuResponse
{
    public string Token { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string CategoryName { get; set; } = "";

    public bool Active { get; set; }

    public List<IngredientResponse>
        Ingredients
    { get; set; } = [];
}