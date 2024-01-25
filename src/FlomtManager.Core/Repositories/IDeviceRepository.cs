using FlomtManager.Core.Models;
using System.Linq.Expressions;

namespace FlomtManager.Core.Repositories
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAll();
        Task<IEnumerable<Device>> GetAll(Expression<Func<Device, bool>> predicate);
        Task<Device> GetById(int id);
        Task Create(Device model);
        Task CreateRange(IEnumerable<Device> models);
        Task Update(Device model);
        Task UpdateRange(IEnumerable<Device> models);
        Task Delete(int id);
        Task DeleteRange(IEnumerable<int> ids);
    }
}
