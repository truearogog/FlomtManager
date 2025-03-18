using System.Data.Common;

namespace FlomtManager.Domain.Abstractions.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
