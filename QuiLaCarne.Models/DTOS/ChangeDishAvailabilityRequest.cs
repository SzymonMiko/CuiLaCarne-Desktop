using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class ChangeDishAvailabilityRequest
{
    public string Token { get; set; } = "";

    public string? UnavailableReason { get; set; }

    public bool Available { get; set; }
}
