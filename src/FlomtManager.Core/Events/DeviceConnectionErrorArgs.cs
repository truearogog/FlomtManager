namespace FlomtManager.Core.Events;

public class DeviceConnectionErrorArgs : EventArgs
{
    public required Exception Exception { get; init; }
}
