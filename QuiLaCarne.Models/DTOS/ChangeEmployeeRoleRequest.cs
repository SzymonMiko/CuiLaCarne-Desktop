using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class ChangeEmployeeRoleRequest
{
    public string EmployeeToken { get; set; } = "";

    public bool Admin { get; set; }
}
