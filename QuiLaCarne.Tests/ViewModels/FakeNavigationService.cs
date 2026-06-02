using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeNavigationService : INavigationService
{
    public List<string> Calls { get; } = [];

    public void ShowLogin() => Calls.Add(nameof(ShowLogin));

    public void ShowMenu() => Calls.Add(nameof(ShowMenu));

    public void ShowUsersPanel() => Calls.Add(nameof(ShowUsersPanel));

    public void ShowTwoFactor() => Calls.Add(nameof(ShowTwoFactor));

    public void CloseCurrentWindow() => Calls.Add(nameof(CloseCurrentWindow));

    public void ShowManagerPanel() => Calls.Add(nameof(ShowManagerPanel));

    public void CloseLogin() => Calls.Add(nameof(CloseLogin));

    public void ShowKitchenMonitor() => Calls.Add(nameof(ShowKitchenMonitor));

    public void ShowPersonnelManagement() => Calls.Add(nameof(ShowPersonnelManagement));

    public void ShowMenuRoomEditor() => Calls.Add(nameof(ShowMenuRoomEditor));

    public void ShowSecurityDashboard() => Calls.Add(nameof(ShowSecurityDashboard));

    public void ShowIngredientConfirmation() => Calls.Add(nameof(ShowIngredientConfirmation));

    public void ShowAdminTwoFactor() => Calls.Add(nameof(ShowAdminTwoFactor));
}
