﻿using FlomtManager.Domain.Abstractions.Data;

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

        foreach (var migration in Migrations)
        {
            await migration.UpInternal(connection, cancellationToken);
        }
    }

    public async Task Drop(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var migration in Migrations.Reverse())
        {
            await migration.DownInternal(connection, cancellationToken);
        }
    }
}
