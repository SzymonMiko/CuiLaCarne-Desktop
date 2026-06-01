using System.Windows;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Services;

public sealed class WpfDialogService : IAppDialogService
{
    private readonly ILocalizationService _localization;

    public WpfDialogService(ILocalizationService localization)
    {
        _localization = localization;
    }

    public void ShowMessage(string message, string title = "")
    {
        MessageBox.Show(_localization.Translate(message), _localization.Translate(title));
    }

    public bool Confirm(string message, string title = "")
    {
        return MessageBox.Show(
            _localization.Translate(message),
            _localization.Translate(title),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
