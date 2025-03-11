using System.Data.Common;

namespace FlomtManager.Core.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
