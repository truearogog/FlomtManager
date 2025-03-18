using FlomtManager.Domain.Enums;

namespace FlomtManager.Domain.Abstractions.DeviceConnection.Events;

public class DeviceConnectionArchiveReadingStateChangedArgs : EventArgs
{
    public required ArchiveReadingState State { get; init; }
    public int LinesRead { get; init; }
}
