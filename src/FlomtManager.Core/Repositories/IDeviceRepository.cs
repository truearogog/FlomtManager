using FlomtManager.Core.Models;

namespace FlomtManager.Core.Repositories
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAll();
        Task<IAsyncEnumerable<Device>> GetAllAsync();
        Task<Device> GetById(int id);
        Task<DeviceDefinition> GetDefinitionByDeviceId(int id);
        Task<int> Create(Device device);
        Task<int> CreateDefinition(DeviceDefinition deviceDefinition);
        Task UpdateDefinitionLastArchiveRead(int deviceId);
        Task Update(Device device);
        Task Delete(int id);
    }
}
