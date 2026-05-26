using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class CreateGuestReportRequest
{
    public string ClientToken { get; set; } = "";

    public string Reason { get; set; } = "";
}