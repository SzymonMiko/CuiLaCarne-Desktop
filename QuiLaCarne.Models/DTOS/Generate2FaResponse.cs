using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class Generate2FaResponse
{
    public string QrCodeImageUrl { get; set; } = "";

    public string ManualEntryKey { get; set; } = "";
}