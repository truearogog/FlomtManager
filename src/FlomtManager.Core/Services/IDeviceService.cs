using FlomtManager.Core.Models;

namespace FlomtManager.Core.Services
{
    public interface IDeviceService
    {
        Task<IEnumerable<Device>> GetAll();
        Task<Device> GetById(int id);
        Task<bool> CreateDevice(Device device);
        Task<bool> UpdateDevice(Device device);
        Task<bool> DeleteDevice(int id);
    }
}
