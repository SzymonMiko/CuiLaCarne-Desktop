using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncReservationResponse
{
    public string Token { get; set; } = "";

    public string TableToken { get; set; } = "";

    public string UserToken { get; set; } = "";

    public DateTimeOffset ReservedFrom { get; set; }

    public DateTimeOffset? ReservedUntil { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
