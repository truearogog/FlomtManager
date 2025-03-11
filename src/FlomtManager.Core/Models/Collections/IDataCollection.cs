using System.Collections;

namespace FlomtManager.Core.Models.Collections;

public interface IDataCollection : IEnumerable
{
    public Type Type { get; }
}
