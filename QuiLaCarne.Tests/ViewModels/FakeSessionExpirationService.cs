using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeSessionExpirationService : ISessionExpirationService
{
    public event EventHandler? SessionExpired;

    public List<string> WatchedTokens { get; } = [];

    public int ClearCount { get; private set; }

    public void StartWatching(string jwt)
    {
        WatchedTokens.Add(jwt);
    }

    public void Clear()
    {
        ClearCount++;
    }

    public void NotifyExpired()
    {
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }
}
