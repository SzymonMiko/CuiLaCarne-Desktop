using System.ComponentModel;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeLocalizationService : ILocalizationService
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage { get; private set; } = "pl";

    public string LanguageSwitchText { get; private set; } = "English";

    public int ToggleCount { get; private set; }

    public int RefreshCount { get; private set; }

    public void ToggleLanguage()
    {
        ToggleCount++;
        CurrentLanguage = CurrentLanguage == "pl" ? "en" : "pl";
        LanguageSwitchText = CurrentLanguage == "pl" ? "English" : "Polski";
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageSwitchText)));
    }

    public string Translate(string text) => $"translated:{text}";

    public void RefreshCurrentView()
    {
        RefreshCount++;
    }
}
