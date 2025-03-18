using FlomtManager.Domain.Enums;

namespace FlomtManager.Domain.Abstractions.DeviceConnection.Events;

public class DeviceConnectionStateChangedArgs : EventArgs
{
    public required ConnectionState State { get; init; }
}
