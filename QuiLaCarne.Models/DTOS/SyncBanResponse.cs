using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncBanResponse
{
    public string Token { get; set; } = "";

    public string UserToken { get; set; } = "";

    public string? BannedByToken { get; set; }

    public string Reason { get; set; } = "";

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool? IsPermanent { get; set; }

    public List<string> StatusTokens { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
