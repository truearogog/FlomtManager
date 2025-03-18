using System.Collections;

namespace FlomtManager.Domain.Models.Collections;

public interface IDataCollection : IEnumerable
{
    public Type Type { get; }
    public int Count { get; }
}
