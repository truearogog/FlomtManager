namespace FlomtManager.Domain.Abstractions.Stores.Events;

public class DeviceIsEditableArgs : EventArgs
{
    public int DeviceId { get; init; }
    public bool IsEditable { get; init; }
}
