using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.Stores.Events;

namespace FlomtManager.Infrastructure.Stores;

internal sealed class DeviceIsEditableStore : IDeviceIsEditableStore
{
    private readonly Dictionary<int, bool> _deviceIsEditable = [];

    public event EventHandler<DeviceIsEditableArgs> DeviceIsEditableUpdated;

    public bool TryGetDeviceIsEditable(int deviceId, out bool isEditable)
    {
        return _deviceIsEditable.TryGetValue(deviceId, out isEditable);
    }

    public void UpdateDeviceIsEditable(int deviceId, bool isEditable)
    {
        _deviceIsEditable[deviceId] = isEditable;
        DeviceIsEditableUpdated?.Invoke(this, new DeviceIsEditableArgs { DeviceId = deviceId, IsEditable = isEditable });
    }
}
