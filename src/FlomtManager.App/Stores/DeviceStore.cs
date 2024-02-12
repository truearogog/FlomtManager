using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;

namespace FlomtManager.App.Stores
{
    public class DeviceStore
    {
        public event Action<Device>? DeviceCreated;
        public event Action<Device>? DeviceUpdated;
        public event Action<int>? DeviceDeleted;

        public async Task<int> CreateDevice(IDeviceRepository deviceRepository, Device device)
        {
            var id = await deviceRepository.Create(device);
            device.Id = id;
            DeviceCreated?.Invoke(device);
            return id;
        }

        public async Task<int> UpdateDevice(IDeviceRepository deviceRepository, Device device)
        {
            var id = await deviceRepository.Update(device);
            device.Id = id;
            DeviceUpdated?.Invoke(device);
            return id;
        }

        public async Task DeleteDevice(IDeviceRepository deviceRepository, int id)
        {
            await deviceRepository.Delete(id);
            DeviceDeleted?.Invoke(id);
        }
    }
}
