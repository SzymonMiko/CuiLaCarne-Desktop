using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class CreateTableRequest
{
    public int TableNumber { get; set; }

    public int Capacity { get; set; }
}