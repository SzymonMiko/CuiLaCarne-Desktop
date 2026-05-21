using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using QuiLaCarne.Models;
using System.Collections.ObjectModel;
namespace QuiLaCarne.ViewModels;

public partial class UsersPanelViewModel : ObservableObject
{
    private readonly QuiLaCarneDbContext _db;

    public ObservableCollection<Users> Users { get; } = [];

    public UsersPanelViewModel(QuiLaCarneDbContext db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        var users =
            await _db.Users
                .OrderBy(x => x.Username)
                .ToListAsync();

        Users.Clear();

        foreach (var user in users)
            Users.Add(user);
    }
}