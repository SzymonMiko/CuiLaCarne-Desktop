using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QuiLaCarne.ViewModels;

public partial class DashboardViewModel
    : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;

    public ObservableCollection<Users> Users
    { get; set; } = [];

    public DashboardViewModel(
        QuiLaCarneDbContext db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        var users =
            await _db.Users.ToListAsync();

        Users.Clear();

        foreach (var user in users)
        {
            Users.Add(user);
        }
    }
}