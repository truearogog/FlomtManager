using System.Collections;

namespace FlomtManager.Domain.Models.Collections;

public class DataCollection<T>(long size) : IDataCollection, IEnumerable<T>
{
    public Type Type => typeof(T);
    public int Count => _values.Length;

    private readonly T[] _values = new T[size];
    public T[] Values => _values;

    public IEnumerator<T> GetEnumerator() => (IEnumerator<T>)_values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
