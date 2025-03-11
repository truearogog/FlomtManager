using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Events;

public class DeviceConnectionStateChangedArgs : EventArgs
{
    public required ConnectionState State { get; init; }
}
