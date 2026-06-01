using System.ComponentModel;

namespace QuiLaCarne.Services.IServices;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; }

    string LanguageSwitchText { get; }

    void ToggleLanguage();

    string Translate(string text);

    void RefreshCurrentView();
}
