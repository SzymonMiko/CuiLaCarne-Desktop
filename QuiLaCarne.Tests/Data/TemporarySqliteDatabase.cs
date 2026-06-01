using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Data;

namespace QuiLaCarne.Tests.Data;

internal sealed class TemporarySqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public QuiLaCarneDbContext Db { get; }

    public TemporarySqliteDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<QuiLaCarneDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new QuiLaCarneDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
