using FlomtManager.Domain.Abstractions.Stores.Events;

namespace FlomtManager.Domain.Abstractions.Stores;

public interface IDeviceIsEditableStore
{
    event EventHandler<DeviceIsEditableArgs> DeviceIsEditableUpdated;
    bool TryGetDeviceIsEditable(int deviceId, out bool isEditable);
    void UpdateDeviceIsEditable(int deviceId, bool isEditable);
}
