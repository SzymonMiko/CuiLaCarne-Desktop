using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncTableResponse
{
    public string Token { get; set; } = "";

    public int TableNumber { get; set; }

    public int Capacity { get; set; }

    public List<string> StatusTokens { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
