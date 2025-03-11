using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Events;

public class DeviceConnectionArchiveReadingStateChangedArgs : EventArgs
{
    public required ArchiveReadingState State { get; init; }
    public int LinesRead { get; init; }
}
