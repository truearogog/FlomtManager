using Dapper;
using FlomtManager.Core.Data;
using FlomtManager.Infrastructure.Data.TypeHandlers;

namespace FlomtManager.Infrastructure.Data;

public sealed class DbInitializer(IDbConnectionFactory connectionFactory) : IDbInitializer
{
    private static readonly Migration[] Migrations = [
        new Migrations.Initial()
    ];

    public async Task Init(CancellationToken cancellationToken = default)
    {
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

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
