namespace FlomtManager.Core.Events;

public class DeviceConnectionArchiveReadingProgressChangedArgs : EventArgs
{
    public required int Progress { get; init; }
    public required int MaxProgress { get; init; }
}
