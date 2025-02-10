using FlomtManager.Core.Entities;

namespace FlomtManager.App.Stores
{
    public class DeviceStore
    {
        public event Action<Device> DeviceCreated;
        public event Action<Device> DeviceUpdated;
        public event Action<int> DeviceDeleted;

        public void CreateDevice(Device device)
        {
            DeviceCreated?.Invoke(device);
        }

        public void UpdateDevice(Device device)
        {
            DeviceUpdated?.Invoke(device);
        }

        public void DeleteDevice(int id)
        {
            DeviceDeleted?.Invoke(id);
        }
    }
}
