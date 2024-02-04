using System.Linq.Expressions;

namespace FlomtManager.Core.Repositories.Base
{
    public interface IRepositoryBase<TModel>
    {
        Task<IEnumerable<TModel>> GetAll();
        Task<IEnumerable<TModel>> GetAll(Expression<Func<TModel, bool>> predicate);
        Task<TModel> GetById(int id);
        Task Create(TModel model);
        Task CreateRange(IEnumerable<TModel> models);
        Task Update(TModel model);
        Task UpdateRange(IEnumerable<TModel> models);
        Task Delete(int id);
        Task DeleteRange(IEnumerable<int> ids);
    }
}
