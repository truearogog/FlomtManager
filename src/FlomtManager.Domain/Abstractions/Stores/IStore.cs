namespace FlomtManager.Domain.Abstractions.Stores;

public interface IStore<T>
{
    event EventHandler<T> Added;
    event EventHandler<T> Updated;
    event EventHandler<T> Removed;

    void Add(T item);
    void Update(T item);
    void Remove(T item);
}