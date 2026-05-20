using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuiLaCarne.Data;

public class QuiLaCarneDbContextFactory
    : IDesignTimeDbContextFactory<QuiLaCarneDbContext>
{
    public QuiLaCarneDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<
                QuiLaCarneDbContext>();



        var dbPath =
            Path.Combine(
                Environment.CurrentDirectory,
                "quilacarne.db");

        optionsBuilder.UseSqlite(
            $"Data Source={dbPath}");

        return new QuiLaCarneDbContext(
            optionsBuilder.Options);

    }
}