using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS
{
    public class AddIngredientRequest
    {
        public AddEntityRequest Entity { get; set; } = new();

        public List<string> AllergenTokens { get; set; } = [];
    }
}
