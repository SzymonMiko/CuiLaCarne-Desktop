using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Services;

public sealed class WpfLocalizationService : ILocalizationService
{
    private readonly Dictionary<string, string> _plToEn;
    private readonly Dictionary<string, string> _enToPl;
    private string _currentLanguage = "pl";

    public event PropertyChangedEventHandler? PropertyChanged;

    public WpfLocalizationService()
    {
        _plToEn = BuildPolishToEnglishDictionary();
        _enToPl = BuildEnglishToPolishDictionary(_plToEn);
    }

    public string CurrentLanguage => _currentLanguage;

    public string LanguageSwitchText => _currentLanguage == "pl" ? "English" : "Polski";

    public void ToggleLanguage()
    {
        _currentLanguage = _currentLanguage == "pl" ? "en" : "pl";
        ApplyCulture();
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(LanguageSwitchText));
        RefreshCurrentView();
    }

    public string Translate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (_currentLanguage == "en")
        {
            return TranslateWithPatterns(text, _plToEn);
        }

        return TranslateWithPatterns(text, _enToPl);
    }

    public void RefreshCurrentView()
    {
        var application = Application.Current;

        if (application == null)
        {
            return;
        }

        application.Dispatcher.InvokeAsync(() =>
        {
            foreach (Window window in application.Windows)
            {
                window.Title = Translate(window.Title);
                TranslateElement(window);
            }
        });
    }

    private void ApplyCulture()
    {
        var culture = new CultureInfo(_currentLanguage == "pl" ? "pl-PL" : "en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private void TranslateElement(DependencyObject element)
    {
        switch (element)
        {
            case TextBlock textBlock:
                TranslateTextBlock(textBlock);
                break;

            case HeaderedContentControl headeredContentControl:
                TranslateHeaderedContentControl(headeredContentControl);
                break;

            case ContentControl contentControl:
                TranslateContentControl(contentControl);
                break;

            case Page page:
                page.Title = Translate(page.Title);
                break;

            case DataGrid dataGrid:
                TranslateDataGrid(dataGrid);
                break;
        }

        foreach (var child in GetChildren(element))
        {
            TranslateElement(child);
        }
    }

    private void TranslateTextBlock(TextBlock textBlock)
    {
        if (!string.IsNullOrWhiteSpace(textBlock.Text) &&
            BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty) == null)
        {
            textBlock.Text = Translate(textBlock.Text);
        }

        foreach (var run in textBlock.Inlines.OfType<Run>())
        {
            run.Text = Translate(run.Text);
        }
    }

    private void TranslateContentControl(ContentControl contentControl)
    {
        if (contentControl.Content is string text &&
            BindingOperations.GetBindingExpression(contentControl, ContentControl.ContentProperty) == null)
        {
            contentControl.Content = Translate(text);
        }
    }

    private void TranslateHeaderedContentControl(HeaderedContentControl headeredContentControl)
    {
        if (headeredContentControl.Header is string header &&
            BindingOperations.GetBindingExpression(headeredContentControl, HeaderedContentControl.HeaderProperty) == null)
        {
            headeredContentControl.Header = Translate(header);
        }
    }

    private void TranslateDataGrid(DataGrid dataGrid)
    {
        foreach (var column in dataGrid.Columns)
        {
            if (column.Header is string header)
            {
                column.Header = Translate(header);
            }
        }
    }

    private static IEnumerable<DependencyObject> GetChildren(DependencyObject parent)
    {
        var visualChildrenCount = parent is Visual
            ? VisualTreeHelper.GetChildrenCount(parent)
            : 0;

        for (var i = 0; i < visualChildrenCount; i++)
        {
            yield return VisualTreeHelper.GetChild(parent, i);
        }

        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;
        }
    }

    private static string TranslateWithPatterns(string text, IReadOnlyDictionary<string, string> dictionary)
    {
        if (dictionary.TryGetValue(text, out var translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Usunąć ", "Delete ", "?", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Delete ", "Usunąć ", "?", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Stolik ", "Table ", "", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Table ", "Stolik ", "", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Wybrano: ", "Selected: ", "", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Selected: ", "Wybrano: ", "", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Wybrany klient: ", "Selected client: ", "", out translated))
        {
            return translated;
        }

        if (TryTranslatePrefix(text, dictionary, "Selected client: ", "Wybrany klient: ", "", out translated))
        {
            return translated;
        }

        return text;
    }

    private static bool TryTranslatePrefix(
        string text,
        IReadOnlyDictionary<string, string> dictionary,
        string sourcePrefix,
        string targetPrefix,
        string suffix,
        out string translated)
    {
        translated = "";

        if (!text.StartsWith(sourcePrefix, StringComparison.Ordinal) ||
            !text.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = text[sourcePrefix.Length..];

        if (suffix.Length > 0)
        {
            value = value[..^suffix.Length];
        }

        translated = $"{targetPrefix}{value}{suffix}";
        return true;
    }

    private static Dictionary<string, string> BuildEnglishToPolishDictionary(IReadOnlyDictionary<string, string> plToEn)
    {
        var enToPl = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in plToEn)
        {
            var polish = pair.Key;
            var english = pair.Value;

            if (!enToPl.TryGetValue(english, out var existingPolish) ||
                (string.Equals(existingPolish, english, StringComparison.Ordinal) &&
                 !string.Equals(polish, english, StringComparison.Ordinal)))
            {
                enToPl[english] = polish;
            }
        }

        return enToPl;
    }

    private static Dictionary<string, string> BuildPolishToEnglishDictionary()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Qui la Carne Panel"] = "Qui la Carne Panel",
            ["Panel Qui la Carne"] = "Qui la Carne Panel",
            ["Panel zarządzania"] = "Management Panel",
            ["Monitor kuchni"] = "Kitchen Display System",
            ["Panel klientów"] = "Clients Panel",
            ["Uwierzytelnianie dwuetapowe"] = "Two Factor Authentication",
            ["Zarządzanie personelem"] = "Personnel Management",
            ["Panel bezpieczeństwa"] = "Security Dashboard",
            ["Potwierdzenie składnika"] = "Ingredient Confirmation",
            ["Administracyjne 2FA"] = "Administrative 2FA",

            ["Wybierz panel roboczy"] = "Choose work panel",
            ["Zarządzanie"] = "Management",
            ["Kuchnia"] = "Kitchen",
            ["Klienci"] = "Clients",
            ["Personel"] = "Personnel",
            ["Bezpieczeństwo"] = "Security",
            ["Składniki"] = "Ingredients",
            ["Admin 2FA"] = "Admin 2FA",
            ["Sprawdź 2FA"] = "2FA Check",
            ["Polski"] = "Polski",
            ["English"] = "English",

            ["Panel logowania personelu"] = "Staff login panel",
            ["Manager"] = "Manager",
            ["Nazwa użytkownika / Email"] = "Username / Email",
            ["Hasło"] = "Password",
            ["Zaloguj"] = "Login",
            ["Weryfikacja dwuetapowa"] = "Two-factor verification",
            ["Wpisz kod z aplikacji uwierzytelniającej"] = "Enter the code from your authenticator app",
            ["Zweryfikuj"] = "Verify",

            ["Edytor "] = "Menu ",
            ["menu"] = "Editor",
            ["Danie"] = "Dish",
            ["Cena"] = "Price",
            ["Dostępne"] = "Available",
            ["Powód blokady"] = "Block reason",
            ["Zapisz"] = "Save",
            ["Zablokuj"] = "Block",
            ["Usuń"] = "Delete",
            ["Wybierz zdjęcie"] = "Choose photo",
            ["Zmień zdjęcie"] = "Change photo",
            ["Dodaj składnik"] = "Add ingredient",
            ["Nazwa PL"] = "Name PL",
            ["Nazwa EN"] = "Name EN",
            ["Alergeny"] = "Allergens",
            ["Dodaj danie"] = "Add dish",
            ["Nazwa dania"] = "Dish name",
            ["Kategoria"] = "Category",
            ["Usuń słownik"] = "Delete lookup",
            ["Mapa "] = "Room ",
            ["sali"] = "Map",
            ["Wybierz stolik"] = "Select a table",
            ["Usuń stolik"] = "Delete table",
            ["Status: -"] = "Status: -",
            ["Czas"] = "Time",
            ["Gość"] = "Guest",
            ["Dania"] = "Dishes",
            ["Stolik"] = "Table",
            ["Miejsca"] = "Seats",
            ["Dodaj stolik"] = "Add table",
            ["Brak statusu"] = "No status",
            ["Brak dań"] = "No dishes",
            ["Nie wybrano zdjęcia"] = "No photo selected",
            ["Nie wybrano nowego zdjęcia"] = "No new photo selected",

            ["Konta "] = "Staff ",
            ["pracowników"] = "Accounts",
            ["Dodawaj pracowników, przypisuj role i zmieniaj dostępność kont."] = "Add employees, assign roles, and change account availability.",
            ["Użytkownik"] = "Username",
            ["Role"] = "Roles",
            ["Aktywny"] = "Active",
            ["Formularz pracownika"] = "Employee form",
            ["Rola"] = "Role",
            ["Dodaj pracownika"] = "Add employee",
            ["Zmień rolę"] = "Change role",
            ["Zablokuj / odblokuj"] = "Block / unblock",
            ["Zmień hasło pracownika"] = "Change employee password",
            ["Nowe hasło"] = "New password",
            ["Potwierdź hasło"] = "Confirm password",
            ["Zmień hasło"] = "Change password",
            ["Pracownik"] = "Staff",

            ["Lista "] = "Clients ",
            ["klientów"] = "List",
            ["Zamówienia i zgłoszenia klientów z lokalnej zsynchronizowanej bazy."] = "Client orders and reports from the local synchronized cache.",
            ["Zamówienia"] = "Orders",
            ["Zgłoszenia"] = "Reports",
            ["Zgłaszający"] = "Reporter",
            ["Opis"] = "Description",

            ["Zarządzanie "] = "Menu ",
            ["Blokuj dania, aktualizuj dostępność i czekaj na potwierdzenie przez WebSocket."] = "Block dishes, update availability, and wait for WebSocket confirmation.",
            ["Powód"] = "Reason",
            ["Wybrane danie"] = "Selected dish",
            ["Zablokuj wybrane"] = "Block selected",
            ["Ustaw dostępne"] = "Make available",
            ["Odśwież"] = "Refresh",

            ["Monitor "] = "Order ",
            ["Zamówień"] = "Monitor",
            ["Kafelkowy system kuchni: Do zrobienia → W trakcie → Gotowe"] = "Kitchen board: To do → In progress → Ready",
            ["Do zrobienia"] = "To do",
            ["W trakcie"] = "In progress",
            ["Gotowe"] = "Ready",
            ["Ilość:"] = "Quantity:",
            ["Czas oczekiwania:"] = "Waiting time:",
            ["Zamówienie:"] = "Order:",
            ["Rezerwacja:"] = "Reservation:",

            ["Panel bezpieczeństwa / SIEM"] = "Security Dashboard / SIEM",
            ["Zdarzenia "] = "Security ",
            ["bezpieczeństwa"] = "events",
            ["Logi audytu: zmiany cen, nieudane logowania, akcje użytkowników i adresy IP, jeśli backend je zwraca."] = "Audit logs: price changes, failed logins, user actions and IP addresses when backend returns them.",
            ["Cache systemu"] = "System cache",
            ["Wyczyść wybrany"] = "Clear selected",
            ["Wyczyść wszystko"] = "Clear all",
            ["Akcja"] = "Action",
            ["Szczegóły"] = "Details",

            ["Brakujący "] = "Missing ",
            ["składnik"] = "ingredient",
            ["Wybierz składnik i potwierdź jego niedostępność. Powiązane dania mogą zostać zablokowane, a WebSocket powiadomi pozostałych klientów."] = "Choose an ingredient and confirm it is unavailable. Related dishes can be blocked and WebSocket will notify other clients.",
            ["Potwierdź brak składnika"] = "Confirm missing ingredient",
            ["Powiązane dania"] = "Affected dishes",

            ["2FA wymagane"] = "2FA required",
            ["wymagane"] = "required",
            ["Konta administracyjne powinny włączyć uwierzytelnianie dwuetapowe przed użyciem wrażliwych paneli."] = "Administrative roles should enable two-factor authentication before using sensitive panels.",
            ["Wygeneruj sekret 2FA / QR"] = "Generate 2FA secret / QR",
            ["Włącz 2FA"] = "Enable 2FA",

            ["Sesja wygasła. Zaloguj się ponownie."] = "Your session has expired. Please log in again.",
            ["Sesja wygasła"] = "Session expired",
            ["Wpisz kod 2FA."] = "Enter 2FA code.",
            ["Synchronizacja zakończona."] = "Sync finished.",
            ["Logowanie nie powiodło się."] = "Login failed.",
            ["Brak dostępu. Aplikacja desktopowa jest tylko dla administratorów."] = "Access denied. Desktop app is only for administrators.",
            ["Najpierw wybierz danie."] = "Choose a dish first.",
            ["Najpierw wybierz pracownika."] = "Select an employee first.",
            ["Najpierw wybierz zdjęcie."] = "Choose a photo first.",
            ["Powód jest wymagany."] = "Reason is required.",
            ["Nazwa składnika jest wymagana."] = "Ingredient name is required.",
            ["Nazwa dania jest wymagana."] = "Dish name is required.",
            ["Wybierz przynajmniej jeden składnik."] = "Choose at least one ingredient.",
            ["Składnik dodany."] = "Ingredient added.",
            ["Danie dodane."] = "Dish added.",
            ["Danie nie zostało usunięte."] = "Dish was not deleted.",
            ["Stolik nie został usunięty."] = "Table was not deleted.",
            ["Stolik usunięty."] = "Table deleted.",
            ["Usuń danie"] = "Delete dish",
            ["Usuń stolik"] = "Delete table",
            ["Wyczyść cache"] = "Clear cache",
            ["Wyczyść wszystkie cache"] = "Clear all caches",
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
