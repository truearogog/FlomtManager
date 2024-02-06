using FlomtManager.Core.Models;

namespace FlomtManager.Core.Services
{
    public interface IDeviceService
    {
        IAsyncEnumerable<Device> GetAll();
        Task<Device> GetById(int id);
        Task<bool> CreateDevice(Device device);
        Task<bool> UpdateDevice(Device device);
        Task<bool> DeleteDevice(int id);
    }
}
