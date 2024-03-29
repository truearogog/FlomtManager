﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using FlomtManager.Core.Models.Base;
using FlomtManager.Core.Repositories.Base;
using FlomtManager.Data.EF.Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlomtManager.Data.EF.Repositories.Base
{
    internal abstract class RepositoryBase<TEntity, TModel>(IAppDb db, DbSet<TEntity> dbSet, IDataMapper mapper) : IRepositoryBase<TModel>
        where TEntity : EntityBase
        where TModel : ModelBase
    {
        protected readonly IAppDb Db = db;
        protected readonly DbSet<TEntity> DbSet = dbSet;
        protected IDataMapper Mapper = mapper;
        protected IConfigurationProvider MapperConfig => Mapper.ConfigurationProvider;

        public async Task<IEnumerable<TModel>> GetAll()
        {
            return await GetAllQueryable()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<TModel>> GetAll(Expression<Func<TModel, bool>> predicate)
        {
            return await GetAllQueryable(predicate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public virtual IQueryable<TModel> GetAllQueryable()
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .ProjectTo<TModel>(MapperConfig);
        }

        public virtual IQueryable<TModel> GetAllQueryable(Expression<Func<TModel, bool>> predicate)
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .ProjectTo<TModel>(MapperConfig)
                .Where(predicate);
        }

        public IAsyncEnumerable<TModel> GetAllAsync()
        {
            return GetAllQueryable().AsAsyncEnumerable();
        }

        public IAsyncEnumerable<TModel> GetAllAsync(Expression<Func<TModel, bool>> predicate)
        {
            return GetAllQueryable(predicate).AsAsyncEnumerable();
        }

        public async Task<TModel?> GetById(int id)
        {
            return await DbSet.AsNoTracking()
                .Where(x => x.Id == id)
                .ProjectTo<TModel>(MapperConfig)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<TModel> Create(TModel model)
        {
            var entity = Mapper.Map<TEntity>(model);
            await DbSet.AddAsync(entity).ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
            return Mapper.Map<TModel>(entity);
        }

        public async Task CreateRange(IEnumerable<TModel> models)
        {
            var entities = Mapper.Map<IEnumerable<TEntity>>(models);
            await Db.InsertRangeAsync(entities).ConfigureAwait(false);
            await Db.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<TModel> Update(TModel model)
        {
            var existingEntity = DbSet.Local.SingleOrDefault(x => x.Id == model.Id);
            if (existingEntity != null)
            {
                Mapper.Map(model, existingEntity);
                existingEntity.Updated = DateTime.Now;
                await Db.SaveChangesAsync().ConfigureAwait(false);
                return Mapper.Map<TModel>(existingEntity);
            }
            else
            {
                var entity = Mapper.Map<TEntity>(model);
                entity.Updated = DateTime.Now;
                DbSet.Entry(entity).State = EntityState.Modified;
                await Db.SaveChangesAsync().ConfigureAwait(false);
                return Mapper.Map<TModel>(entity);
            }
        }

        public async Task UpdateRange(IEnumerable<TModel> models)
        {
            var entities = Mapper.Map<IEnumerable<TEntity>>(models);
            foreach (var entity in entities)
            {
                entity.Updated = DateTime.Now;
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

        public async Task<bool> Any(Expression<Func<TModel, bool>> predicate)
        {
            return await GetAllQueryable(predicate).AnyAsync();
        }
    }
}
