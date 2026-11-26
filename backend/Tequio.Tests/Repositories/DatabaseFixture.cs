using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Tequio.Infrastructure.Models;
using Xunit;

namespace Tequio.Tests.Repositories;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        await ExecuteSqlScriptsAsync(_msSqlContainer.GetConnectionString());

        var builder = new SqlConnectionStringBuilder(_msSqlContainer.GetConnectionString())
        {
            InitialCatalog = "TequioDB"
        };
        ConnectionString = builder.ConnectionString;
    }

    public TequioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TequioDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new TequioDbContext(options);
    }

    private static async Task ExecuteSqlScriptsAsync(string masterConnectionString)
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "Scripts", "StoredProcedures");
        var sqlFiles = Directory.GetFiles(scriptsDirectory, "*.sql")
                                .OrderBy(f => f);

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        foreach (var file in sqlFiles)
        {
            var script = await File.ReadAllTextAsync(file);
            var batches = Regex.Split(
                script,
                @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = batch;
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
