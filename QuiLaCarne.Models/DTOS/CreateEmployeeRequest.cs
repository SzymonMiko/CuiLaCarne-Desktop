using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class CreateEmployeeRequest
{
    public RegisterRequest Register { get; set; } = new();

    public bool Admin { get; set; }
}