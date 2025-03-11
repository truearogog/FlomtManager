using FlomtManager.Core.Models;

namespace FlomtManager.Core.Stores;

public interface IDeviceStore
{
    event EventHandler<Device> DeviceAdded;
    event EventHandler<Device> DeviceUpdated;
    event EventHandler<Device> DeviceRemoved;

    void Add(Device device);
    void Update(Device device);
    void Remove(Device device);
}
