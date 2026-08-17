using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvestManagement.Infrastructure.Tests;

internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private const string ConnectionStringVariable =
        "INVEST_MANAGEMENT_TEST_CONNECTION";

    private readonly string _databaseName;
    private readonly string _adminConnectionString;

    private PostgreSqlTestDatabase(
        string databaseName,
        string adminConnectionString,
        InvestManagementDbContext dbContext)
    {
        _databaseName = databaseName;
        _adminConnectionString = adminConnectionString;
        DbContext = dbContext;
    }

    public InvestManagementDbContext DbContext { get; }

    public static async Task<PostgreSqlTestDatabase> CreateAsync()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringVariable} to a dedicated PostgreSQL " +
                "test database connection string.");
        }

        var testBuilder = new NpgsqlConnectionStringBuilder(
            configuredConnectionString);

        if (string.IsNullOrWhiteSpace(testBuilder.Database) ||
            !testBuilder.Database.Contains(
                "test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The database name in {ConnectionStringVariable} must " +
                "contain 'test'.");
        }

        var databaseName = $"{testBuilder.Database}_{Guid.NewGuid():N}";
        var adminBuilder = new NpgsqlConnectionStringBuilder(testBuilder.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using (var adminConnection =
                     new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var createCommand = adminConnection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await createCommand.ExecuteNonQueryAsync();
        }

        testBuilder.Database = databaseName;
        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseNpgsql(testBuilder.ConnectionString)
            .Options;
        var dbContext = new InvestManagementDbContext(options);

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch
        {
            await dbContext.DisposeAsync();
            await DropDatabaseAsync(adminBuilder.ConnectionString, databaseName);
            throw;
        }

        return new PostgreSqlTestDatabase(
            databaseName,
            adminBuilder.ConnectionString,
            dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await DropDatabaseAsync(_adminConnectionString, _databaseName);
    }

    private static async Task DropDatabaseAsync(
        string adminConnectionString,
        string databaseName)
    {
        await using var adminConnection = new NpgsqlConnection(adminConnectionString);
        await adminConnection.OpenAsync();
        await using var dropCommand = adminConnection.CreateCommand();
        dropCommand.CommandText =
            $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await dropCommand.ExecuteNonQueryAsync();
    }
}
