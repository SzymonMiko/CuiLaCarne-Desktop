using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncOrderItemsResponse
{
    public string Token { get; set; } = "";

    public string OrderItemToken
    {
        get => Token;
        set => Token = value;
    }

    public string OrderToken { get; set; } = "";

    public string ReservationToken
    {
        get => OrderToken;
        set => OrderToken = value;
    }

    public string DishToken { get; set; } = "";

    public string MenuItemToken
    {
        get => DishToken;
        set => DishToken = value;
    }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
