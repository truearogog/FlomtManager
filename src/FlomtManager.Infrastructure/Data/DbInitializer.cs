using System.Transactions;
using Dapper;
using FlomtManager.Domain.Abstractions.Data;
using FlomtManager.Domain.Constants;

namespace FlomtManager.Infrastructure.Data;

public sealed class DbInitializer(IDbConnectionFactory connectionFactory) : IDbInitializer
{
    private static readonly Migration[] Migrations = [
        new Migrations.Initial()
    ];

    public async Task Init(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS Migrations (
                Name NVARCHAR(100) PRIMARY KEY NOT NULL,
                AppliedAt {SqlTypes.DateTime} NOT NULL
            );
            """);

        foreach (var migration in Migrations)
        {
            var exists = await connection.ExecuteScalarAsync<bool>(
                "SELECT COUNT(*) FROM Migrations WHERE Name = @Name;",
                new { migration.Name });

            if (!exists)
            {
                await migration.UpInternal(connection, cancellationToken);

                await connection.ExecuteAsync(
                "INSERT INTO Migrations (Name, AppliedAt) VALUES (@Name, @AppliedAt);",
                new { migration.Name, AppliedAt = DateTime.UtcNow });
            }
        }
    }

    public async Task Drop(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if ((await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Migrations';")) != 0)
        {
            foreach (var migration in Migrations.Reverse())
            {
                var exists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(*) FROM Migrations WHERE Name = @Name;",
                    new { migration.Name });

                if (exists)
                {
                    await migration.DownInternal(connection, cancellationToken);

                    await connection.ExecuteAsync(
                        "DELETE FROM Migrations WHERE Name = @Name;",
                        new { migration.Name });
                }
            }

            await connection.ExecuteAsync("DROP TABLE IF EXISTS Migrations;");
        }
    }
}
