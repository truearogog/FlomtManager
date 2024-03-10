using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;

namespace FlomtManager.App.Stores
{
    public class DeviceStore
    {
        public event Action<Device>? DeviceCreated;
        public event Action<Device>? DeviceUpdated;
        public event Action<int>? DeviceDeleted;

        public async Task CreateDevice(IDeviceRepository deviceRepository, Device device)
        {
            var createdDevice = await deviceRepository.Create(device);
            DeviceCreated?.Invoke(createdDevice);
        }

        public async Task UpdateDevice(IDeviceRepository deviceRepository, Device device)
        {
            var updatedDevice = await deviceRepository.Update(device);
            DeviceUpdated?.Invoke(updatedDevice);
        }

        public async Task DeleteDevice(IDeviceRepository deviceRepository, int id)
        {
            await deviceRepository.Delete(id);
            DeviceDeleted?.Invoke(id);
        }
    }
}
