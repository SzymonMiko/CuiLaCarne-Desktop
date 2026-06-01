using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public MenuViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void OpenManager()
    {
        _navigation.ShowManagerPanel();
    }

    [RelayCommand]
    private void OpenKitchen()
    {
        _navigation.ShowKitchenMonitor();
    }

    [RelayCommand]
    private void OpenUsers()
    {
        _navigation.ShowUsersPanel();
    }

    [RelayCommand]
    private void OpenPersonnel()
    {
        _navigation.ShowPersonnelManagement();
    }

    [RelayCommand]
    private void OpenMenuRoom()
    {
        _navigation.ShowManagerPanel();
    }

    [RelayCommand]
    private void OpenSecurity()
    {
        _navigation.ShowSecurityDashboard();
    }

    [RelayCommand]
    private void OpenIngredientConfirmation()
    {
        _navigation.ShowIngredientConfirmation();
    }

    [RelayCommand]
    private void OpenAdminTwoFactor()
    {
        _navigation.ShowAdminTwoFactor();
    }

    [RelayCommand]
    private void OpenTwoFactor()
    {
        _navigation.ShowTwoFactor();
    }
}
