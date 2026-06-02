using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;
using Xunit;

namespace QuiLaCarne.Tests.Data;

public sealed class QuiLaCarneDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ConfiguresSqliteProvider()
    {
        var factory = new QuiLaCarneDbContextFactory();

        using var db = factory.CreateDbContext([]);

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
    }
}
