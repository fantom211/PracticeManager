using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticeManager.Api.Data;

namespace PracticeManager.Tests;

/// <summary>
/// Shared helpers for the optional PostgreSQL integration tests. They silently skip when the
/// local database or the revision migration is not available, so `dotnet test` stays green in a
/// clean checkout.
/// </summary>
public static class TestDatabase
{
    public const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5433;Database=practice_portal;Username=postgres;Password=postgres";

    public const string RequiredMigration = "20261121222210_DocumentSoftDeleteAndTeacherComments";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("PRACTICE_PORTAL_TEST_CONNECTION")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? DefaultConnectionString;

    public static DbContextOptions<AppDbContext> Options =>
        new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;

    public static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsReadyAsync()
    {
        if (!await CanConnectAsync())
        {
            return false;
        }

        try
        {
            await using var db = new AppDbContext(Options);
            var applied = await db.Database.GetAppliedMigrationsAsync();
            return applied.Contains(RequiredMigration, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection;
