﻿using FlomtManager.Core.Models.Base;
using System.Linq.Expressions;

namespace FlomtManager.Core.Repositories.Base
{
    public interface IRepositoryBase<TModel>
        where TModel : ModelBase
    {
        Task<IEnumerable<TModel>> GetAll();
        Task<IEnumerable<TModel>> GetAll(Expression<Func<TModel, bool>> predicate);
        IQueryable<TModel> GetAllQueryable();
        IQueryable<TModel> GetAllQueryable(Expression<Func<TModel, bool>> predicate);
        IAsyncEnumerable<TModel> GetAllAsync();
        IAsyncEnumerable<TModel> GetAllAsync(Expression<Func<TModel, bool>> predicate);
        Task<TModel?> GetById(int id);
        Task<TModel> Create(TModel model);
        Task CreateRange(IEnumerable<TModel> models);
        Task<TModel> Update(TModel model);
        Task UpdateRange(IEnumerable<TModel> models);
        Task Delete(int id);
        Task DeleteRange(IEnumerable<int> ids);
        Task<bool> Any(Expression<Func<TModel, bool>> predicate);
    }
}
