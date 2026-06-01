using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;

namespace QuiLaCarne.ViewModels;

public partial class PersonnelManagementViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly UserService _userService;
    private readonly SyncService _syncService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;
    private readonly IAppDialogService _dialog;
    private readonly IUiDispatcherService _uiDispatcher;

    public ObservableCollection<EmployeeRow> Employees { get; } = new();
    public ObservableCollection<string> AvailableRoles { get; } = new(["Pracownik", "Admin"]);

    private EmployeeRow? selectedEmployee;
    public EmployeeRow? SelectedEmployee
    {
        get => selectedEmployee;
        set => SetProperty(ref selectedEmployee, value);
    }

    private string newUsername = "";
    public string NewUsername
    {
        get => newUsername;
        set => SetProperty(ref newUsername, value);
    }

    private string newEmail = "";
    public string NewEmail
    {
        get => newEmail;
        set => SetProperty(ref newEmail, value);
    }

    private string newPassword = "";
    public string NewPassword
    {
        get => newPassword;
        set => SetProperty(ref newPassword, value);
    }

    private string employeePassword = "";
    public string EmployeePassword
    {
        get => employeePassword;
        set => SetProperty(ref employeePassword, value);
    }

    private string employeeConfirmPassword = "";
    public string EmployeeConfirmPassword
    {
        get => employeeConfirmPassword;
        set => SetProperty(ref employeeConfirmPassword, value);
    }

    private bool isAdmin;
    public bool IsAdmin
    {
        get => isAdmin;
        set => SetProperty(ref isAdmin, value);
    }

    private string selectedRole = "Pracownik";
    public string SelectedRole
    {
        get => selectedRole;
        set
        {
            value ??= "Pracownik";

            if (SetProperty(ref selectedRole, value))
            {
                IsAdmin = string.Equals(value, "Admin", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public PersonnelManagementViewModel(
        QuiLaCarneDbContext db,
        UserService userService,
        SyncService syncService,
        IRealtimeUpdateService realtimeUpdateService,
        IAppDialogService dialog,
        IUiDispatcherService uiDispatcher)
    {
        _db = db;
        _userService = userService;
        _syncService = syncService;
        _realtimeUpdateService = realtimeUpdateService;
        _dialog = dialog;
        _uiDispatcher = uiDispatcher;
        _realtimeUpdateService.LocalDataChanged += OnRealtimeDataChanged;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!string.IsNullOrWhiteSpace(SessionService.JwtToken))
        {
            await _syncService.SyncRolesAsync(SessionService.JwtToken);
            await _syncService.SyncUsersAsync(SessionService.JwtToken);
        }

        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .OrderBy(x => x.Username)
            .ToListAsync();

        Employees.Clear();

        foreach (var user in users)
        {
            Employees.Add(new EmployeeRow
            {
                Token = user.Token,
                Username = user.Username,
                Email = user.Email,
                RolesText = string.Join(", ", user.Roles.Select(x => x.Name).OrderBy(x => x)),
                IsEnabled = user.IsEnabled
            });
        }
    }

    [RelayCommand]
    private async Task AddEmployeeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) ||
            string.IsNullOrWhiteSpace(NewEmail) ||
            string.IsNullOrWhiteSpace(NewPassword))
        {
            _dialog.ShowMessage("Nazwa użytkownika, email i hasło są wymagane.");
            return;
        }

        await _userService.AddEmployeeAsync(
            SessionService.JwtToken,
            new CreateEmployeeRequest
            {
                Admin = IsAdmin,
                Register = new RegisterRequest
                {
                    Username = NewUsername,
                    Email = NewEmail,
                    Password = NewPassword,
                    ConfirmPassword = NewPassword
                }
            });

        NewUsername = "";
        NewEmail = "";
        NewPassword = "";
        IsAdmin = false;
        SelectedRole = "Pracownik";

        _dialog.ShowMessage("Zmiana pracownika wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    [RelayCommand]
    private async Task ChangeRoleAsync()
    {
        if (SelectedEmployee == null)
        {
            return;
        }

        await _userService.ChangeEmployeeRoleAsync(
            SessionService.JwtToken,
            SelectedEmployee.Token,
            IsAdmin);

        _dialog.ShowMessage("Zmiana roli wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    [RelayCommand]
    private async Task ChangeAvailabilityAsync()
    {
        if (SelectedEmployee == null)
        {
            return;
        }

        await _userService.ChangeEmployeeAvailabilityAsync(
            SessionService.JwtToken,
            SelectedEmployee.Token,
            available: !SelectedEmployee.IsEnabled);

        _dialog.ShowMessage("Zmiana dostępności wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    [RelayCommand]
    private async Task ChangeEmployeePasswordAsync()
    {
        if (SelectedEmployee == null)
        {
            _dialog.ShowMessage("Najpierw wybierz pracownika.");
            return;
        }

        if (string.Equals(SelectedEmployee.Username, SessionService.Username, StringComparison.OrdinalIgnoreCase))
        {
            _dialog.ShowMessage("Manager nie może tutaj zmienić własnego hasła.");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmployeePassword) ||
            string.IsNullOrWhiteSpace(EmployeeConfirmPassword))
        {
            _dialog.ShowMessage("Hasło i potwierdzenie są wymagane.");
            return;
        }

        if (!string.Equals(EmployeePassword, EmployeeConfirmPassword, StringComparison.Ordinal))
        {
            _dialog.ShowMessage("Hasła nie są takie same.");
            return;
        }

        await _userService.ChangeEmployeePasswordAsync(
            SessionService.JwtToken,
            SelectedEmployee.Token,
            EmployeePassword,
            EmployeeConfirmPassword);

        EmployeePassword = "";
        EmployeeConfirmPassword = "";

        _dialog.ShowMessage("Hasło pracownika zostało zmienione.");
    }

    [RelayCommand]
    private async Task DeleteEmployeeAsync()
    {
        if (SelectedEmployee == null)
        {
            return;
        }

        await _userService.DeleteEmployeeAsync(
            SessionService.JwtToken,
            SelectedEmployee.Token);

        _dialog.ShowMessage("Prośba o usunięcie wysłana. Lista odświeży się po potwierdzeniu z serwera.");
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        if (e.EntityType != "EMPLOYEE")
        {
            return;
        }

        _ = _uiDispatcher.InvokeAsync(LoadAsync);
    }
}

public class EmployeeRow
{
    public string Token { get; set; } = "";

    public string Username { get; set; } = "";

    public string Email { get; set; } = "";

    public string RolesText { get; set; } = "";

    public bool IsEnabled { get; set; }
}
