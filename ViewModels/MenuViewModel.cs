using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuiLaCarne.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace QuiLaCarne.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public MenuViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void OpenUsersPanel()
    {
        _navigation.ShowUsersPanel();
    }

    [RelayCommand]
    private void OpenManager()
    {
        _navigation.ShowManagerPanel();
    }
   

    [RelayCommand]
    private void OpenKitchen()
    {
        // later: _navigation.ShowKitchenPanel();
    }
}
