using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Services.Api;
public static class SessionService
{
    public static string JwtToken { get; set; } = "";

    public static string RefreshToken { get; set; } = "";

    public static string Username { get; set; } = "";

    public static bool IsAdmin { get; set; }
}
