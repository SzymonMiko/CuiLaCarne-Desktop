using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

public class SyncReservationResponse
{
    public string Token { get; set; } = "";

    public string ReservationToken
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

    public List<string> StatusTokens { get; set; } = [];

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public DateTimeOffset ReservedFrom
    {
        get => StartTime;
        set => StartTime = value;
    }

    public DateTimeOffset? ReservedUntil
    {
        get => EndTime;
        set => EndTime = value;
    }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
