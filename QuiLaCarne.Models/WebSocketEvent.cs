using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace QuiLaCarne.Models;

public class WebSocketEvent
{
    public string EventType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Token { get; set; } = "";
    public JsonElement? Payload { get; set; }
    public DateTime Timestamp { get; set; }
}