using FlomtManager.Core.DeviceConnection;
using FlomtManager.Core.Models;

namespace FlomtManager.Core.ViewModels;

public interface IDeviceConnectionFactory
{
    IDeviceConnection Create(Device device);
}
