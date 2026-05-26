using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class ChangeEmployeePasswordRequest
{
    public string EmployeeToken { get; set; } = "";

    public string Password { get; set; } = "";

    public string ConfirmPassword { get; set; } = "";
}
