using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Models.DTOS;
using QuiLaCarne.Services.Api;
using QuiLaCarne.Services.IServices;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuiLaCarne.ViewModels;

public partial class PersonnelManagementViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly UserService _userService;
    private readonly SyncService _syncService;
    private readonly IRealtimeUpdateService _realtimeUpdateService;

    public ObservableCollection<EmployeeRow> Employees { get; } = new();
    public ObservableCollection<string> AvailableRoles { get; } = new(["Staff", "Admin"]);

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

    private string selectedRole = "Staff";
    public string SelectedRole
    {
        get => selectedRole;
        set
        {
            value ??= "Staff";

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
        IRealtimeUpdateService realtimeUpdateService)
    {
        _db = db;
        _userService = userService;
        _syncService = syncService;
        _realtimeUpdateService = realtimeUpdateService;
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
            MessageBox.Show("Username, email and password are required.");
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
        SelectedRole = "Staff";

        MessageBox.Show("Employee change sent. The list will refresh after the server confirms it.");
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

        MessageBox.Show("Role change sent. The list will refresh after the server confirms it.");
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

        MessageBox.Show("Availability change sent. The list will refresh after the server confirms it.");
    }

    [RelayCommand]
    private async Task ChangeEmployeePasswordAsync()
    {
        if (SelectedEmployee == null)
        {
            MessageBox.Show("Select an employee first.");
            return;
        }

        if (string.Equals(SelectedEmployee.Username, SessionService.Username, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Managers cannot change their own password here.");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmployeePassword) ||
            string.IsNullOrWhiteSpace(EmployeeConfirmPassword))
        {
            MessageBox.Show("Password and confirmation are required.");
            return;
        }

        if (!string.Equals(EmployeePassword, EmployeeConfirmPassword, StringComparison.Ordinal))
        {
            MessageBox.Show("Passwords do not match.");
            return;
        }

        await _userService.ChangeEmployeePasswordAsync(
            SessionService.JwtToken,
            SelectedEmployee.Token,
            EmployeePassword,
            EmployeeConfirmPassword);

        EmployeePassword = "";
        EmployeeConfirmPassword = "";

        MessageBox.Show("Employee password changed.");
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

        MessageBox.Show("Delete request sent. The list will refresh after the server confirms it.");
    }

    private void OnRealtimeDataChanged(object? sender, WebSocketEvent e)
    {
        if (e.EntityType != "EMPLOYEE")
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(async () =>
        {
            await LoadAsync();
        });
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
