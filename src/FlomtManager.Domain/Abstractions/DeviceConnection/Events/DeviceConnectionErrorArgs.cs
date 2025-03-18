namespace FlomtManager.Domain.Abstractions.DeviceConnection.Events;

public class DeviceConnectionErrorArgs : EventArgs
{
    public required Exception Exception { get; init; }
}
