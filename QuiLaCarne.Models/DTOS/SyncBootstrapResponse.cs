using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncBootstrapResponse
{
    public Dictionary<string, SyncBootstrapModule>
        Modules
    { get; set; } = [];

    public DateTimeOffset ServerTime { get; set; }
}

public class SyncBootstrapModule
{
    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int PageSize { get; set; }
}