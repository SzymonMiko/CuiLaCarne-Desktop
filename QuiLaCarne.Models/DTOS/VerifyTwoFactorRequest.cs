using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class VerifyTwoFactorRequest
{
    public string PreAuthToken { get; set; } = "";

    public string Code { get; set; } = "";
}
