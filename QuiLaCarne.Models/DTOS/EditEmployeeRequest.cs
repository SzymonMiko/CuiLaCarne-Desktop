using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class EditEmployeeRequest
{
    public string EmployeeToken { get; set; } = "";

    public string? Email { get; set; }

    public string? UserName { get; set; }
}