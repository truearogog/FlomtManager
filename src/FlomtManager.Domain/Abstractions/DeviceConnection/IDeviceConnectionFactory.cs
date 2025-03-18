using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.DeviceConnection;

public interface IDeviceConnectionFactory
{
    IDeviceConnection Create(Device device);
}
