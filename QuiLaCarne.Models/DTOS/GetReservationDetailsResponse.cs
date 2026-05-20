using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class ReservationDetailsResponse
{
    public string Token { get; set; } = "";

    public DateTimeOffset ReservedFrom { get; set; }

    public DateTimeOffset? ReservedUntil { get; set; }

    public string TableToken { get; set; } = "";

    public int TableNumber { get; set; }

    public string UserToken { get; set; } = "";

    public string Username { get; set; } = "";

    public decimal TotalPrice { get; set; }

    public List<ReservationDishResponse> Dishes { get; set; } = [];

    public List<string> Statuses { get; set; } = [];
}
