using FlomtManager.Domain.Abstractions.Stores;

namespace FlomtManager.Infrastructure.Stores;

internal abstract class Store<T> : IStore<T>
{
    public event EventHandler<T> Added;
    public event EventHandler<T> Updated;
    public event EventHandler<T> Removed;

    public void Add(T item)
    {
        Added?.Invoke(this, item);
    }

    public void Update(T item)
    {
        Updated?.Invoke(this, item);
    }

    public void Remove(T item)
    {
        Removed?.Invoke(this, item);
    }
}
