using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.Stores;

public interface IDeviceStore
{
    event EventHandler<Device> DeviceAdded;
    event EventHandler<Device> DeviceUpdated;
    event EventHandler<Device> DeviceRemoved;

    void Add(Device device);
    void Update(Device device);
    void Remove(Device device);
}
