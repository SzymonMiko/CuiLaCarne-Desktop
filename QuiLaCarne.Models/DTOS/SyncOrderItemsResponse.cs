using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncOrderItemsResponse
{
    public string Token { get; set; } = "";

    public string OrderToken { get; set; } = "";

    public string DishToken { get; set; } = "";

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
