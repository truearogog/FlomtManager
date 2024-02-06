using AutoMapper;
using AutoMapper.QueryableExtensions;
using FlomtManager.Core.Repositories.Base;
using FlomtManager.Data.EF.Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlomtManager.Data.EF.Repositories.Base
{
    internal abstract class RepositoryBase<TEntity, TModel>(IAppDb db, DbSet<TEntity> dbSet, IDataMapper mapper) : IRepositoryBase<TModel>
        where TEntity : EntityBase
    {
        protected readonly IAppDb Db = db;
        protected readonly DbSet<TEntity> DbSet = dbSet;
        protected IDataMapper Mapper = mapper;
        protected IConfigurationProvider MapperConfig => Mapper.ConfigurationProvider;

        public IAsyncEnumerable<TModel> GetAll()
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .ProjectTo<TModel>(MapperConfig)
                .AsAsyncEnumerable();
        }

        public IAsyncEnumerable<TModel> GetAll(Expression<Func<TModel, bool>> predicate)
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .ProjectTo<TModel>(MapperConfig)
                .Where(predicate)
                .AsAsyncEnumerable();
        }

        public async Task<TModel> GetById(int id)
        {
            return await DbSet.AsNoTracking()
                .Where(x => x.Id == id)
                .ProjectTo<TModel>(MapperConfig)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task Create(TModel model)
        {
            var entity = Mapper.Map<TEntity>(model);
            await DbSet.AddAsync(entity).ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task CreateRange(IEnumerable<TModel> models)
        {
            var entities = Mapper.Map<IEnumerable<TEntity>>(models);
            await Db.InsertRangeAsync(entities).ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task Update(TModel model)
        {
            var entity = Mapper.Map<TEntity>(model);
            entity.Updated = DateTime.UtcNow;
            DbSet.Update(entity);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task UpdateRange(IEnumerable<TModel> models)
        {
            var entities = Mapper.Map<IEnumerable<TEntity>>(models);
            foreach (var entity in entities)
            {
                entity.Updated = DateTime.UtcNow;
            }
            await Db.UpdateRangeAsync(entities);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task Delete(int id)
        {
            await DbSet.Where(x => x.Id == id).ExecuteDeleteAsync().ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteRange(IEnumerable<int> ids)
        {
            await DbSet.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync().ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
