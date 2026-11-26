using Invelop.Contacts.Application.Contacts;
using Invelop.Contacts.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Application.Tests;

/// <summary>
/// A real SQLite database held in memory, created by running the actual migrations.
/// </summary>
/// <remarks>
/// Not the EF in-memory provider: that one does not enforce constraints, does not translate the
/// same LINQ, and would let a broken projection pass. This runs the migrations the reviewer runs,
/// against the engine the application ships with, so a query that works here works in the app.
/// The connection is held open because an in-memory SQLite database exists only as long as a
/// connection to it does.
/// </remarks>
internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private TestDatabase(SqliteConnection connection, AppDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync(TimeProvider timeProvider)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var context = NewContext(connection, timeProvider);
        await context.Database.MigrateAsync();

        return new TestDatabase(connection, context);
    }

    /// <summary>
    /// A second context over the same database, for the cases where it matters that an entity is
    /// loaded fresh rather than served from the first context's change tracker.
    /// </summary>
    public AppDbContext NewContext(TimeProvider timeProvider) => NewContext(connection, timeProvider);

    /// <summary>
    /// Writes to the database behind EF Core's back, the way a hand-run UPDATE, a restored backup
    /// or another service sharing the schema would. Returns the rows affected: a corruption test
    /// whose UPDATE quietly matched nothing would pass for the wrong reason.
    /// </summary>
    public async Task<int> ExecuteRawSqlAsync(string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> QueryScalarAsync(string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return await command.ExecuteScalarAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }

    private static AppDbContext NewContext(SqliteConnection connection, TimeProvider timeProvider) =>
        new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options,
            new ContactValidator(timeProvider));
}
