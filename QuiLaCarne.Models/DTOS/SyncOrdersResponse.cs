using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncOrderResponse
{
    public string Token { get; set; } = "";

    public string OrderToken
    {
        get => Token;
        set => Token = value;
    }

    public string TableToken { get; set; } = "";

    public string UserToken { get; set; } = "";

    public string ClientToken
    {
        get => UserToken;
        set => UserToken = value;
    }

    public string CustomerToken
    {
        get => UserToken;
        set => UserToken = value;
    }

    public string GuestToken
    {
        get => UserToken;
        set => UserToken = value;
    }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
