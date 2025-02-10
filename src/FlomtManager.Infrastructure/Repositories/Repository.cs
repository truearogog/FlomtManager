using FlomtManager.Core;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Infrastructure.Repositories;

internal abstract class Repository<T>(IAppDb appDb, DbSet<T> table) : IRepository<T> where T : class, IEntity
{
    public void Add(T entity)
    {
        table.Add(entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        table.AddRange(entities);
    }

    public void Remove(T entity)
    {
        table.Remove(entity);
    }

    public IQueryable<T> GetAll()
    {
        return table;
    }

    public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await table.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<T> GetByIdAsyncNonTracking(int id, CancellationToken cancellationToken = default)
    {
        return await table.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }


    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await appDb.SaveChangesAsync(cancellationToken);
    }
}
