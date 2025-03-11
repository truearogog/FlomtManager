using FlomtManager.Core.Models;
using FlomtManager.Core.Stores;

namespace FlomtManager.Infrastructure.Stores;

internal sealed class DeviceStore : IDeviceStore
{
    public event EventHandler<Device> DeviceAdded;
    public event EventHandler<Device> DeviceUpdated;
    public event EventHandler<Device> DeviceRemoved;

    public void Add(Device device)
    {
        DeviceAdded?.Invoke(this, device);
    }

    public void Update(Device device)
    {
        DeviceUpdated?.Invoke(this, device);
    }

    public void Remove(Device device)
    {
        DeviceRemoved?.Invoke(this, device);
    }
}
