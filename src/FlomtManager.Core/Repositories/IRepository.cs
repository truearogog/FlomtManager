using FlomtManager.Core.Entities;

namespace FlomtManager.Core.Repositories;

public interface IRepository<T> where T : IEntity
{
    IQueryable<T> GetAll();
    Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T> GetByIdAsyncNonTracking(int id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
