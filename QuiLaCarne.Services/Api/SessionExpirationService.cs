using System.Net;
using System.Text;
using System.Text.Json;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Services.Api;

public sealed class SessionExpirationService : DelegatingHandler, ISessionExpirationService
{
    private readonly object _syncRoot = new();
    private Timer? _expirationTimer;
    private bool _expirationHandled;

    public event EventHandler? SessionExpired;

    public void StartWatching(string jwt)
    {
        lock (_syncRoot)
        {
            _expirationTimer?.Dispose();
            _expirationTimer = null;
            _expirationHandled = false;

            var expiresAt = TryGetExpirationUtc(jwt);
            if (expiresAt is null)
            {
                return;
            }

            var dueTime = expiresAt.Value - DateTimeOffset.UtcNow;
            if (dueTime <= TimeSpan.Zero)
            {
                NotifyExpired();
                return;
            }

            _expirationTimer = new Timer(
                _ => NotifyExpired(),
                null,
                dueTime,
                Timeout.InfiniteTimeSpan);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _expirationTimer?.Dispose();
            _expirationTimer = null;
            _expirationHandled = false;
        }

        SessionService.JwtToken = "";
        SessionService.RefreshToken = "";
        SessionService.IsAdmin = false;
    }

    public void NotifyExpired()
    {
        var shouldRaise = false;

        lock (_syncRoot)
        {
            if (_expirationHandled)
            {
                return;
            }

            _expirationHandled = true;
            _expirationTimer?.Dispose();
            _expirationTimer = null;
            shouldRaise = true;
        }

        SessionService.JwtToken = "";
        SessionService.RefreshToken = "";
        SessionService.IsAdmin = false;

        if (shouldRaise)
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (request.Headers.Authorization is not null
            && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            NotifyExpired();
        }

        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _expirationTimer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static DateTimeOffset? TryGetExpirationUtc(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = DecodeBase64Url(parts[1]);
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("exp", out var exp))
            {
                return null;
            }

            long? unixSeconds = null;

            if (exp.ValueKind == JsonValueKind.Number
                && exp.TryGetInt64(out var numericValue))
            {
                unixSeconds = numericValue;
            }
            else if (exp.ValueKind == JsonValueKind.String
                     && long.TryParse(exp.GetString(), out var stringValue))
            {
                unixSeconds = stringValue;
            }

            return unixSeconds is null
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value);
        }
        catch
        {
            return null;
        }
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
