using System.Windows;
using QuiLaCarne.Models;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Services;

public sealed class RealtimeNotificationService
{
    private const int MaxVisibleToasts = 4;
    private const double ToastMargin = 14;
    private const double ToastWidth = 360;
    private const double ToastHeight = 104;
    private readonly List<NotificationToastWindow> _toasts = [];

    public RealtimeNotificationService(IRealtimeUpdateService realtimeUpdateService)
    {
        realtimeUpdateService.LocalDataChanged += OnLocalDataChanged;
    }

    private void OnLocalDataChanged(object? sender, WebSocketEvent websocketEvent)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            ShowToast(BuildTitle(websocketEvent), BuildMessage(websocketEvent));
        });
    }

    private void ShowToast(string title, string message)
    {
        while (_toasts.Count >= MaxVisibleToasts)
        {
            _toasts[0].CloseWithAnimation();
            _toasts.RemoveAt(0);
        }

        var toast = new NotificationToastWindow(title, message);
        toast.Closed += (_, _) =>
        {
            _toasts.Remove(toast);
            RepositionVisibleToasts();
        };

        _toasts.Add(toast);

        RepositionVisibleToasts();
        var (left, top) = GetToastPosition(_toasts.Count - 1);
        toast.ShowAt(left, top);
    }

    private void RepositionVisibleToasts()
    {
        for (var i = 0; i < _toasts.Count; i++)
        {
            if (!_toasts[i].IsVisible)
            {
                continue;
            }

            var (left, top) = GetToastPosition(i);
            _toasts[i].MoveTo(left, top);
        }
    }

    private static (double Left, double Top) GetToastPosition(int index)
    {
        var workArea = SystemParameters.WorkArea;
        var left = workArea.Right - ToastWidth - ToastMargin;
        var top = workArea.Bottom - ((ToastHeight + ToastMargin) * (index + 1));

        return (left, Math.Max(workArea.Top + ToastMargin, top));
    }

    public static string BuildTitle(WebSocketEvent websocketEvent)
    {
        var entity = LabelEntity(websocketEvent.EntityType);
        var action = LabelEvent(websocketEvent.EventType);

        return string.IsNullOrWhiteSpace(entity)
            ? "Aktualizacja systemu"
            : $"{entity}: {action}";
    }

    public static string BuildMessage(WebSocketEvent websocketEvent)
    {
        var entity = LabelEntity(websocketEvent.EntityType);
        var token = ShortToken(websocketEvent.Token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return string.IsNullOrWhiteSpace(entity)
                ? "Dane lokalne zostały odświeżone po zmianie z serwera."
                : $"{entity} zostało odświeżone z serwera.";
        }

        return $"{entity} zostało odświeżone z serwera. Token: {token}";
    }

    private static string LabelEvent(string eventType)
    {
        return eventType.ToUpperInvariant() switch
        {
            "CREATED" => "dodano",
            "UPDATED" => "zmieniono",
            "DELETED" => "usunięto",
            "SYNCED" => "zsynchronizowano",
            "" => "zmiana",
            _ => eventType.ToLowerInvariant()
        };
    }

    private static string LabelEntity(string entityType)
    {
        return entityType.ToUpperInvariant() switch
        {
            "DISH" => "Danie",
            "DISH_AVAILABILITY" => "Dostępność dania",
            "MENU_AVAILABILITY" => "Dostępność menu",
            "CATEGORY" => "Kategoria",
            "INGREDIENT" => "Składnik",
            "ORDER" => "Zamówienie",
            "ORDER_ITEM" => "Pozycja zamówienia",
            "ORDER_STATUS" => "Status zamówienia",
            "ORDER_ITEM_STATUS" => "Status pozycji",
            "RESERVATION" => "Rezerwacja",
            "TABLE" => "Stolik",
            "TABLE_STATUS" => "Status stolika",
            "EMPLOYEE" => "Pracownik",
            "BAN" => "Blokada klienta",
            "REPORT" => "Zgłoszenie",
            "" => "",
            _ => entityType
        };
    }

    private static string ShortToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "";
        }

        return token.Length <= 12 ? token : $"{token[..8]}...";
    }
}
