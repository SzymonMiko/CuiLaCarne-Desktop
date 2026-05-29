namespace QuiLaCarne.Services.IServices;

public interface ISessionExpirationService
{
    event EventHandler? SessionExpired;

    void StartWatching(string jwt);

    void Clear();

    void NotifyExpired();
}
