using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using System;
using System.Threading.Tasks;

namespace FlomtManager.UI.Stores
{
    public class DeviceStore
    {
        public event Action<Device> DeviceCreated;
        public event Action<Device> DeviceUpdated;
        public event Action<int> DeviceDeleted;

        public async Task<bool> CreateDevice(IDeviceService service, Device device)
        {
            var created = await service.CreateDevice(device);
            if (created)
            {
                DeviceCreated?.Invoke(device);
            }
            return created;
        }

        public async Task<bool> UpdateDevice(IDeviceService service, Device device)
        {
            var updated = await service.UpdateDevice(device);
            if (updated)
            {
                DeviceUpdated?.Invoke(device);
            }
            return updated;
        }

        public async Task<bool> DeleteDevice(IDeviceService service, int id)
        {
            var deleted = await service.DeleteDevice(id);
            if (deleted)
            {
                DeviceDeleted?.Invoke(id);
            }
            return deleted;
        }
    }
}
