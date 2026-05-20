using QuiLaCarne.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class LoginResponse
{
    public LoginData? Data { get; set; }

    public string Message { get; set; } = "";

    public int StatusCode { get; set; }

    public List<string>? ErrorMessages { get; set; }

    public bool Success { get; set; }
}

