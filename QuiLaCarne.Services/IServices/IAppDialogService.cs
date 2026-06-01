namespace QuiLaCarne.Services.IServices;

public interface IAppDialogService
{
    void ShowMessage(string message, string title = "");

    bool Confirm(string message, string title = "");
}
