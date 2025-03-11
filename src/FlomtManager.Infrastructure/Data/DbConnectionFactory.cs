using System.Data.Common;
using FlomtManager.Core.Data;
using Microsoft.Data.Sqlite;

namespace FlomtManager.Infrastructure.Data;

public sealed class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection()
    {
        return new SqliteConnection(connectionString);
    }
}
