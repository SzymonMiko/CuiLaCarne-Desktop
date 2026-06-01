using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeAppDialogService : IAppDialogService
{
    public List<string> Messages { get; } = [];

    public bool ConfirmResult { get; set; } = true;

    public void ShowMessage(string message, string title = "")
    {
        Messages.Add(message);
    }

    public bool Confirm(string message, string title = "")
    {
        Messages.Add(message);
        return ConfirmResult;
    }
}
