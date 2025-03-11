using System.Data;
using System.Data.Common;

namespace FlomtManager.Infrastructure.Data;

internal abstract class Migration
{
    protected abstract Task Up(DbConnection connection, IDbTransaction transaction);

    internal async Task UpInternal(DbConnection connection, CancellationToken cancellationToken = default)
    {
        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await Up(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected abstract Task Down(DbConnection connection, IDbTransaction transaction);

    internal async Task DownInternal(DbConnection connection, CancellationToken cancellationToken = default)
    {
        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await Down(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
