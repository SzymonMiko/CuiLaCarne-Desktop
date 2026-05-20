using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class SyncUserResponse
{
    public string Token { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public bool? IsActive { get; set; }

    public bool IsStaff { get; set; }

    public List<string> RoleTokens { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
