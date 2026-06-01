using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using Xunit;

namespace QuiLaCarne.Tests.Data;

public sealed class QuiLaCarneDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_SetsTimestampsForNewEntity()
    {
        using var database = new TemporarySqliteDatabase();
        var user = new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.com",
            PasswordHash = "hash",
            IsEnabled = true
        };

        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();

        Assert.NotEqual(default, user.CreatedAt);
        Assert.NotEqual(default, user.UpdatedAt);
        Assert.True(user.UpdatedAt >= user.CreatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_UpdatesUpdatedAtForModifiedEntity()
    {
        using var database = new TemporarySqliteDatabase();
        var oldTimestamp = DateTimeOffset.UtcNow.AddDays(-2);
        var user = new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.com",
            PasswordHash = "hash",
            CreatedAt = oldTimestamp,
            UpdatedAt = oldTimestamp
        };
        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();

        user.Email = "new@example.com";
        await database.Db.SaveChangesAsync();

        Assert.True(user.UpdatedAt > user.CreatedAt);
    }

    [Fact]
    public async Task UsersRoles_ManyToManyRelationshipPersists()
    {
        using var database = new TemporarySqliteDatabase();
        var role = new Roles
        {
            Token = "ROLE_CLIENT",
            Name = "Client"
        };
        var user = new Users
        {
            Token = "client-token",
            Username = "client",
            Email = "client@example.com",
            PasswordHash = "hash",
            Roles = [role]
        };

        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var loaded = await database.Db.Users
            .Include(x => x.Roles)
            .SingleAsync(x => x.Token == "client-token");

        Assert.Equal("Client", Assert.Single(loaded.Roles).Name);
    }

    [Fact]
    public async Task UserEmailUniqueIndexRejectsDuplicateEmail()
    {
        using var database = new TemporarySqliteDatabase();
        database.Db.Users.AddRange(
            new Users
            {
                Token = "client-1",
                Username = "client1",
                Email = "same@example.com",
                PasswordHash = "hash"
            },
            new Users
            {
                Token = "client-2",
                Username = "client2",
                Email = "same@example.com",
                PasswordHash = "hash"
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Db.SaveChangesAsync());
    }
}
