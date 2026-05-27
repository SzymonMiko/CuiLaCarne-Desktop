namespace QuiLaCarne.Models.DTOS;

public class SyncGuestReportResponse
{
    public string Token { get; set; } = "";

    public string GuestToken { get; set; } = "";

    public string ReporterToken { get; set; } = "";

    public List<string> StatusTokens { get; set; } = [];

    public string Reason { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
