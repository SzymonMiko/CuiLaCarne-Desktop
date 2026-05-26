using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using QuiLaCarne.Services.Api;
using System.Collections.ObjectModel;
using System.Windows;

public partial class PersonnelManagementViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;
    private readonly EmployeeManagementService _employeeService;
    private readonly SyncService _syncService;

    public ObservableCollection<EmployeeRow> Employees { get; } = new();
    public ObservableCollection<string> AvailableRoles { get; } = new() { "WAITER", "COOK", "ADMIN", "MANAGER" };

    [ObservableProperty] private EmployeeRow? selectedEmployee;
    [ObservableProperty] private string newUsername = "";
    [ObservableProperty] private string newEmail = "";
    [ObservableProperty] private string newPassword = "";
    [ObservableProperty] private string selectedRole = "WAITER";

    public PersonnelManagementViewModel(QuiLaCarneDbContext db, EmployeeManagementService employeeService, SyncService syncService)
    {
        _db = db;
        _employeeService = employeeService;
        _syncService = syncService;
        _ = LoadEmployeesAsync();
    }

    [RelayCommand]
    public async Task LoadEmployeesAsync()
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync();

        Employees.Clear();
        foreach (var user in users)
        {
            Employees.Add(new EmployeeRow
            {
                Token = user.Token,
                Username = user.Username,
                Email = user.Email,
                IsEnabled = user.IsEnabled,
                RolesText = string.Join(", ", user.Roles.Select(r => r.Name))
            });
        }
    }

    [RelayCommand]
    private async Task AddEmployeeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewPassword))
        {
            MessageBox.Show("Fill username, email and password.");
            return;
        }

        await _employeeService.AddEmployeeAsync(SessionService.JwtToken, NewUsername, NewEmail, NewPassword, SelectedRole);
        await _syncService.SyncUsersAsync(SessionService.JwtToken);
        await LoadEmployeesAsync();
    }

    [RelayCommand]
    private async Task ChangeRoleAsync()
    {
        if (SelectedEmployee == null) return;
        await _employeeService.ChangeRoleAsync(SessionService.JwtToken, SelectedEmployee.Token, SelectedRole);
        await _syncService.SyncUsersAsync(SessionService.JwtToken);
        await LoadEmployeesAsync();
    }

    [RelayCommand]
    private async Task ToggleBlockAsync()
    {
        if (SelectedEmployee == null) return;
        await _employeeService.ChangeAvailabilityAsync(SessionService.JwtToken, SelectedEmployee.Token, !SelectedEmployee.IsEnabled);
        await _syncService.SyncUsersAsync(SessionService.JwtToken);
        await LoadEmployeesAsync();
    }
}

public partial class EmployeeRow : ObservableObject
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string RolesText { get; set; } = "";
    public bool IsEnabled { get; set; }
}
