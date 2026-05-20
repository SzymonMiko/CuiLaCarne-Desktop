using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class ReservationDishResponse
{
    public string DishToken { get; set; } = "";

    public string DishName { get; set; } = "";

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}
