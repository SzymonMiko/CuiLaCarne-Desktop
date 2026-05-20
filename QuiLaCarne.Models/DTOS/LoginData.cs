using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LoginData
{
    public string Token { get; set; } = "";

    public string RefreshToken { get; set; } = "";

    public string Username { get; set; } = "";

    public bool Requires2fa { get; set; }
}
